using System.Security.Claims;
using System.Text;
using System.Text.Encodings.Web;
using Business.Application.Services;
using Business.Infrastructure.Data;
using Business.Infrastructure.Identity;
using Business.Web.Services;
using Business.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;

namespace Business.Web.Controllers;

[Authorize(Policy = AppPolicies.CanViewUsers)]
public class UsersController : Controller
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly ApplicationDbContext _context;
    private readonly IAdminRecoveryCodeService _recoveryCodeService;
    private readonly IEmailSender _emailSender;
    private readonly ILogger<UsersController> _logger;
    private readonly IWebHostEnvironment _environment;

    public UsersController(
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole> roleManager,
        ApplicationDbContext context,
        IAdminRecoveryCodeService recoveryCodeService,
        IEmailSender emailSender,
        ILogger<UsersController> logger,
        IWebHostEnvironment environment)
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _context = context;
        _recoveryCodeService = recoveryCodeService;
        _emailSender = emailSender;
        _logger = logger;
        _environment = environment;
    }

    public async Task<IActionResult> Index()
    {
        var users = await _userManager.Users.OrderBy(x => x.FullName).ToListAsync();
        var model = new List<UserListItemViewModel>();

        foreach (var user in users)
        {
            var roles = await _userManager.GetRolesAsync(user);
            model.Add(new UserListItemViewModel
            {
                Id = user.Id,
                FullName = user.FullName,
                Email = user.Email,
                IsActive = user.IsActive,
                EmailConfirmed = user.EmailConfirmed,
                TwoFactorEnabled = user.TwoFactorEnabled,
                RolesText = roles.Count == 0 ? "-" : string.Join(", ", roles)
            });
        }

        return View(model);
    }

    [Authorize(Roles = AppRoles.Admin)]
    public async Task<IActionResult> Create()
    {
        return View(new UserInviteViewModel { AvailableRoles = await GetRoleNamesAsync() });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = AppRoles.Admin)]
    public async Task<IActionResult> Create(UserInviteViewModel model, CancellationToken cancellationToken)
    {
        if (!await _roleManager.RoleExistsAsync(model.Role))
        {
            ModelState.AddModelError(nameof(model.Role), "Geçerli bir rol seçin.");
        }

        if (!ModelState.IsValid)
        {
            model.AvailableRoles = await GetRoleNamesAsync();
            return View(model);
        }

        var user = new ApplicationUser
        {
            UserName = model.Email.Trim(),
            Email = model.Email.Trim(),
            EmailConfirmed = false,
            FullName = model.FullName.Trim(),
            IsActive = false
        };

        var result = await _userManager.CreateAsync(user);
        if (!result.Succeeded)
        {
            AddIdentityErrors(result);
            model.AvailableRoles = await GetRoleNamesAsync();
            return View(model);
        }

        var roleResult = await _userManager.AddToRoleAsync(user, model.Role);
        if (!roleResult.Succeeded)
        {
            AddIdentityErrors(roleResult);
            model.AvailableRoles = await GetRoleNamesAsync();
            return View(model);
        }

        var inviteLink = await CreateInviteLinkAsync(user);
        try
        {
            await SendInvitationEmailAsync(user, inviteLink, cancellationToken);
            TempData["Success"] = $"{user.Email} adresine kullanıcı daveti gönderildi.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Kullanıcı daveti e-postası gönderilemedi. UserId: {UserId}", user.Id);
            TempData["Error"] = "Kullanıcı oluşturuldu ancak davet e-postası gönderilemedi. SMTP ayarlarını kontrol edin.";
        }

        return RedirectToAction(nameof(Index));
    }

    [Authorize(Policy = AppPolicies.CanManageUsers)]
    public async Task<IActionResult> Edit(string id)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user is null)
        {
            return NotFound();
        }

        var roles = await _userManager.GetRolesAsync(user);
        var claims = await _userManager.GetClaimsAsync(user);
        ViewBag.Departments = await GetDepartmentsAsync();
        ViewBag.CanManageRoles = CanManageRoles();
        return View(new UserFormViewModel
        {
            Id = user.Id,
            FullName = user.FullName ?? string.Empty,
            Email = user.Email ?? string.Empty,
            PhoneNumber = user.PhoneNumber,
            DepartmentId = user.DepartmentId,
            IsActive = user.IsActive,
            SelectedRoles = roles.ToList(),
            SelectedPermissions = claims.Where(x => x.Type == AppClaimTypes.Permission).Select(x => x.Value).ToList(),
            AvailableRoles = await GetRoleNamesAsync()
        });
    }

    [Authorize(Roles = AppRoles.Admin)]
    public async Task<IActionResult> DeleteBlockers(string id, bool wasDeactivated = false, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user is null)
        {
            return NotFound();
        }

        var items = await GetUserDeletionBlockersAsync(user, cancellationToken);
        if (items.Count == 0)
        {
            TempData["Success"] = "Kullanıcının silinmesini engelleyen kayıt bulunamadı.";
            return RedirectToAction(nameof(Index));
        }

        return View(new UserDeletionBlockersViewModel
        {
            UserId = user.Id,
            UserDisplayName = user.FullName ?? user.Email ?? user.UserName ?? user.Id,
            WasDeactivated = wasDeactivated,
            Items = items
        });
    }

    [Authorize(Roles = AppRoles.Admin)]
    public async Task<IActionResult> DownloadDeleteBlockers(string id, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user is null)
        {
            return NotFound();
        }

        var items = await GetUserDeletionBlockersAsync(user, cancellationToken);
        var rows = items
            .Select(item => (IReadOnlyList<object?>)
            [
                item.RowNumber,
                item.Category,
                item.Source,
                item.RecordKey,
                item.Title,
                item.Description
            ])
            .ToList();

        var bytes = ExcelWorkbookBuilder.Build(
        [
            new ExcelSheet(
                "Silme Engelleri",
                ["Sıra", "Kategori", "Kaynak", "Kayıt No", "Başlık", "Açıklama"],
                rows)
        ]);

        return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"kullanici-silme-engelleri-{DateTime.Now:yyyyMMdd-HHmm}.xlsx");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = AppRoles.Admin)]
    public async Task<IActionResult> ClearDeleteBlockerRelation(string userId, Guid recordId, string relationType, CancellationToken cancellationToken)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user is null)
        {
            return NotFound();
        }

        switch (relationType)
        {
            case "task":
            {
                var task = await _context.ProjectTasks
                    .Include(x => x.Assignments)
                    .FirstOrDefaultAsync(x => x.Id == recordId, cancellationToken);
                if (task is null)
                {
                    TempData["Error"] = "Görev kaydı bulunamadı.";
                    break;
                }

                if (task.AssignedToUserId == user.Id)
                {
                    task.AssignedToUserId = null;
                }

                if (task.ResponsibleUserId == user.Id)
                {
                    task.ResponsibleUserId = null;
                }

                _context.ProjectTaskAssignments.RemoveRange(task.Assignments.Where(x => x.UserId == user.Id));
                await _context.SaveChangesAsync(cancellationToken);
                TempData["Success"] = "Görev ilişkileri kaldırıldı.";
                break;
            }
            case "material-request":
            {
                var materialRequest = await _context.MaterialRequests.FirstOrDefaultAsync(x => x.Id == recordId, cancellationToken);
                if (materialRequest is null)
                {
                    TempData["Error"] = "İhtiyaç kaydı bulunamadı.";
                    break;
                }

                materialRequest.RequestedByUserId = null;
                await _context.SaveChangesAsync(cancellationToken);
                TempData["Success"] = "İhtiyaç kaydındaki kullanıcı ilişkisi kaldırıldı.";
                break;
            }
            case "purchase-order":
            {
                var purchaseOrder = await _context.PurchaseOrders.FirstOrDefaultAsync(x => x.Id == recordId, cancellationToken);
                if (purchaseOrder is null)
                {
                    TempData["Error"] = "Sipariş kaydı bulunamadı.";
                    break;
                }

                purchaseOrder.RequestedBy ??= user.FullName ?? user.Email ?? user.UserName;
                purchaseOrder.RequestedByUserId = null;
                await _context.SaveChangesAsync(cancellationToken);
                TempData["Success"] = "Sipariş kaydındaki kullanıcı ilişkisi kaldırıldı.";
                break;
            }
            default:
                TempData["Error"] = "Desteklenmeyen ilişki tipi.";
                break;
        }

        return RedirectToAction(nameof(DeleteBlockers), new { id = userId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = AppRoles.Admin)]
    public async Task<IActionResult> Delete(string id, CancellationToken cancellationToken)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user is null)
        {
            return NotFound();
        }

        if (user.Id == _userManager.GetUserId(User))
        {
            TempData["Error"] = "Kendi kullanıcınızı silemezsiniz.";
            return RedirectToAction(nameof(Index));
        }

        if (await _userManager.IsInRoleAsync(user, AppRoles.Admin) && !await CanApplyRoleChangesAsync(user, []))
        {
            TempData["Error"] = "Sistemde en az bir aktif yönetici kalmalıdır.";
            return RedirectToAction(nameof(Index));
        }

        var blockers = await GetUserDeletionBlockersAsync(user, cancellationToken);
        if (blockers.Count > 0)
        {
            user.IsActive = false;
            await _userManager.UpdateAsync(user);
            await _userManager.UpdateSecurityStampAsync(user);
            return RedirectToAction(nameof(DeleteBlockers), new { id = user.Id, wasDeactivated = true });
        }

        await CleanupUserOwnedDataAsync(user, cancellationToken);

        var result = await _userManager.DeleteAsync(user);
        if (!result.Succeeded)
        {
            AddIdentityErrors(result);
            TempData["Error"] = "Kullanıcı silinemedi. Kayıt ilişkileri kontrol edildi.";
        }
        else
        {
            TempData["Success"] = "Kullanıcı ve kişisel kayıtları silindi.";
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = AppRoles.Admin)]
    public async Task<IActionResult> ResetTwoFactor(string id)
    {
        var currentUser = await _userManager.GetUserAsync(User);
        if (currentUser is null || !currentUser.IsActive || !await _userManager.IsInRoleAsync(currentUser, AppRoles.Admin))
        {
            return Forbid();
        }

        var user = await _userManager.FindByIdAsync(id);
        if (user is null)
        {
            return NotFound();
        }

        if (user.Id == currentUser.Id)
        {
            TempData["Error"] = "Kendi 2FA ayarınızı kullanıcı listesinden sıfırlayamazsınız. 2FA Ayarları ekranını kullanın.";
            return RedirectToAction(nameof(Index));
        }

        if (!user.IsActive || !await _userManager.IsInRoleAsync(user, AppRoles.Admin))
        {
            TempData["Error"] = "2FA sıfırlama yalnızca aktif admin kullanıcılar için yapılabilir.";
            return RedirectToAction(nameof(Index));
        }

        await _userManager.SetTwoFactorEnabledAsync(user, false);
        await _userManager.ResetAuthenticatorKeyAsync(user);
        await _userManager.UpdateSecurityStampAsync(user);

        TempData["Success"] = $"{user.Email} için 2FA sıfırlandı. Kullanıcı bir sonraki girişte yeniden kurulum yapmalıdır.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = AppRoles.Admin)]
    public async Task<IActionResult> ResendInvitation(string id, CancellationToken cancellationToken)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user is null)
        {
            return NotFound();
        }

        if (user.EmailConfirmed || user.IsActive)
        {
            TempData["Error"] = "Davet bağlantısı yalnızca henüz aktifleştirilmemiş kullanıcılar için gönderilebilir.";
            return RedirectToAction(nameof(Index));
        }

        try
        {
            var inviteLink = await CreateInviteLinkAsync(user);
            await SendInvitationEmailAsync(user, inviteLink, cancellationToken);
            TempData["Success"] = $"{user.Email} adresine davet bağlantısı yeniden gönderildi.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Davet bağlantısı yeniden gönderilemedi. UserId: {UserId}", user.Id);
            TempData["Error"] = "Davet bağlantısı gönderilemedi. SMTP ayarlarını kontrol edin.";
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = AppRoles.Admin)]
    public async Task<IActionResult> SendPasswordResetLink(string id, CancellationToken cancellationToken)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user is null)
        {
            return NotFound();
        }

        if (!user.IsActive || !user.EmailConfirmed)
        {
            TempData["Error"] = "Şifre sıfırlama linki yalnızca aktif ve e-postası doğrulanmış kullanıcılara gönderilebilir.";
            return RedirectToAction(nameof(Index));
        }

        try
        {
            var resetLink = await CreatePasswordResetLinkAsync(user);
            await SendPasswordResetEmailAsync(user, resetLink, cancellationToken);
            TempData["Success"] = $"{user.Email} adresine şifre sıfırlama bağlantısı gönderildi.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Şifre sıfırlama bağlantısı gönderilemedi. UserId: {UserId}", user.Id);
            TempData["Error"] = "Şifre sıfırlama bağlantısı gönderilemedi. SMTP ayarlarını kontrol edin.";
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = AppPolicies.CanManageUsers)]
    public async Task<IActionResult> Edit(string id, UserFormViewModel model)
    {
        if (id != model.Id)
        {
            return BadRequest();
        }

        if (!ModelState.IsValid)
        {
            model.AvailableRoles = await GetRoleNamesAsync();
            ViewBag.Departments = await GetDepartmentsAsync();
            ViewBag.CanManageRoles = CanManageRoles();
            return View(model);
        }

        var user = await _userManager.FindByIdAsync(id);
        if (user is null)
        {
            return NotFound();
        }

        user.FullName = model.FullName.Trim();
        user.Email = model.Email.Trim();
        user.UserName = model.Email.Trim();
        user.PhoneNumber = model.PhoneNumber;
        user.DepartmentId = model.DepartmentId;
        user.IsActive = model.IsActive;

        var result = await _userManager.UpdateAsync(user);
        if (!result.Succeeded)
        {
            AddIdentityErrors(result);
            model.AvailableRoles = await GetRoleNamesAsync();
            ViewBag.Departments = await GetDepartmentsAsync();
            ViewBag.CanManageRoles = CanManageRoles();
            return View(model);
        }

        if (CanManageRoles() && !await CanApplyRoleChangesAsync(user, model.SelectedRoles))
        {
            ModelState.AddModelError(string.Empty, "Sistemde en az bir aktif yönetici kalmalıdır.");
            model.AvailableRoles = await GetRoleNamesAsync();
            ViewBag.Departments = await GetDepartmentsAsync();
            ViewBag.CanManageRoles = true;
            return View(model);
        }

        if (CanManageRoles())
        {
            await SyncRolesAsync(user, model.SelectedRoles);
            await SyncUserPermissionsAsync(user, model.SelectedPermissions);
        }

        await _userManager.UpdateSecurityStampAsync(user);
        return RedirectToAction(nameof(Index));
    }

    [Authorize(Policy = AppPolicies.CanManageUsers)]
    public async Task<IActionResult> ResetPassword(string id)
    {
        await Task.CompletedTask;
        TempData["Error"] = "Kullanıcı şifresi admin tarafından belirlenemez. Kullanıcı davet bağlantısı üzerinden kendi şifresini belirlemelidir.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = AppPolicies.CanManageUsers)]
    public async Task<IActionResult> ResetPassword(string id, UserPasswordResetViewModel model)
    {
        await Task.CompletedTask;
        TempData["Error"] = "Kullanıcı şifresi admin tarafından belirlenemez. Kullanıcı davet bağlantısı üzerinden kendi şifresini belirlemelidir.";
        return RedirectToAction(nameof(Index));
    }

    [Authorize(Roles = AppRoles.Admin)]
    public async Task<IActionResult> GenerateRecoveryCodes(string id)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user is null)
        {
            return NotFound();
        }

        if (!user.IsActive || !await _userManager.IsInRoleAsync(user, AppRoles.Admin))
        {
            TempData["Error"] = "Kurtarma kodu yalnızca yönetici kullanıcılar için üretilebilir.";
            return RedirectToAction(nameof(Index));
        }

        return View(new GenerateAdminRecoveryCodesViewModel
        {
            UserId = user.Id,
            FullName = user.FullName ?? string.Empty,
            Email = user.Email ?? string.Empty,
            Count = 2,
            ExpiresAt = DateTime.Today.AddYears(1)
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = AppRoles.Admin)]
    public async Task<IActionResult> GenerateRecoveryCodes(string id, GenerateAdminRecoveryCodesViewModel model)
    {
        if (id != model.UserId)
        {
            return BadRequest();
        }

        var user = await _userManager.FindByIdAsync(id);
        if (user is null)
        {
            return NotFound();
        }

        model.FullName = user.FullName ?? string.Empty;
        model.Email = user.Email ?? string.Empty;

        if (!user.IsActive || !await _userManager.IsInRoleAsync(user, AppRoles.Admin))
        {
            ModelState.AddModelError(string.Empty, "Kurtarma kodu yalnızca yönetici kullanıcılar için üretilebilir.");
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var plainCodes = _recoveryCodeService.GeneratePlainCodes(model.Count);
        foreach (var code in plainCodes)
        {
            var salt = _recoveryCodeService.CreateSalt();
            _context.AdminRecoveryCodes.Add(new AdminRecoveryCode
            {
                UserId = user.Id,
                Salt = salt,
                CodeHash = _recoveryCodeService.HashCode(code, salt),
                ExpiresAt = model.ExpiresAt,
                Note = "Admin panelinden üretildi"
            });
        }

        await _context.SaveChangesAsync();

        return View("GeneratedRecoveryCodes", new GeneratedAdminRecoveryCodesViewModel
        {
            FullName = model.FullName,
            Email = model.Email,
            Codes = plainCodes
        });
    }

    private async Task<List<string>> GetRoleNamesAsync()
    {
        return await _roleManager.Roles.OrderBy(x => x.Name).Select(x => x.Name!).ToListAsync();
    }

    private bool CanManageRoles()
    {
        return User.IsInRole(AppRoles.Admin) ||
               User.HasClaim(AppClaimTypes.Permission, AppPermissions.RolesManage);
    }

    private async Task<string> CreateInviteLinkAsync(ApplicationUser user)
    {
        var emailToken = await _userManager.GenerateEmailConfirmationTokenAsync(user);
        var passwordToken = await _userManager.GeneratePasswordResetTokenAsync(user);
        var encodedEmailToken = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(emailToken));
        var encodedPasswordToken = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(passwordToken));

        return Url.Page(
            "/Account/AcceptInvitation",
            pageHandler: null,
            values: new { area = "Identity", userId = user.Id, emailToken = encodedEmailToken, passwordToken = encodedPasswordToken },
            protocol: Request.Scheme) ?? string.Empty;
    }

    private async Task<string> CreatePasswordResetLinkAsync(ApplicationUser user)
    {
        var token = await _userManager.GeneratePasswordResetTokenAsync(user);
        var encodedToken = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));

        return Url.Page(
            "/Account/ResetPassword",
            pageHandler: null,
            values: new { area = "Identity", userId = user.Id, token = encodedToken },
            protocol: Request.Scheme) ?? string.Empty;
    }

    private async Task SendInvitationEmailAsync(ApplicationUser user, string inviteLink, CancellationToken cancellationToken)
    {
        await _emailSender.SendAsync(new EmailMessage
        {
            To = user.Email!,
            Subject = "FirmaTakip kullanıcı daveti",
            HtmlBody = $"""
                <p>Merhaba {HtmlEncoder.Default.Encode(user.FullName ?? user.Email!)},</p>
                <p>FirmaTakip sistemine erişiminiz için kullanıcı daveti oluşturuldu.</p>
                <p><a href="{HtmlEncoder.Default.Encode(inviteLink)}">Hesabımı doğrula ve şifremi belirle</a></p>
                <p>Bu bağlantı geçersiz veya süresi dolmuşsa sistem yöneticinizden yeni davet isteyin.</p>
                """,
            TextBody = $"""
                Merhaba {user.FullName ?? user.Email!},

                FirmaTakip sistemine erişiminiz için kullanıcı daveti oluşturuldu.
                Davet linki: {inviteLink}

                Bu bağlantı geçersiz veya süresi dolmuşsa sistem yöneticinizden yeni davet isteyin.
                """
        }, cancellationToken);
    }

    private async Task SendPasswordResetEmailAsync(ApplicationUser user, string resetLink, CancellationToken cancellationToken)
    {
        await _emailSender.SendAsync(new EmailMessage
        {
            To = user.Email!,
            Subject = "FirmaTakip şifre sıfırlama",
            HtmlBody = $"""
                <p>Merhaba {HtmlEncoder.Default.Encode(user.FullName ?? user.Email ?? "Kullanıcı")},</p>
                <p>FirmaTakip hesabınız için şifre sıfırlama bağlantısı oluşturuldu.</p>
                <p><a href="{HtmlEncoder.Default.Encode(resetLink)}">Yeni şifre belirle</a></p>
                <p>Bu işlemi siz başlatmadıysanız sistem yöneticinizle görüşün.</p>
                """,
            TextBody = $"""
                Merhaba {user.FullName ?? user.Email ?? "Kullanıcı"},

                FirmaTakip hesabınız için şifre sıfırlama bağlantısı oluşturuldu.
                Şifre sıfırlama linki: {resetLink}

                Bu işlemi siz başlatmadıysanız sistem yöneticinizle görüşün.
                """
        }, cancellationToken);
    }

    private async Task SyncRolesAsync(ApplicationUser user, List<string> selectedRoles)
    {
        var currentRoles = await _userManager.GetRolesAsync(user);
        var selected = selectedRoles.Distinct().ToList();
        await _userManager.RemoveFromRolesAsync(user, currentRoles.Except(selected));
        await _userManager.AddToRolesAsync(user, selected.Except(currentRoles));
    }

    private async Task<bool> CanApplyRoleChangesAsync(ApplicationUser targetUser, List<string> selectedRoles)
    {
        var keepsAdminRole = selectedRoles.Contains(AppRoles.Admin);
        if (keepsAdminRole && targetUser.IsActive)
        {
            return true;
        }

        var admins = await _userManager.GetUsersInRoleAsync(AppRoles.Admin);
        return admins.Any(user => user.Id != targetUser.Id && user.IsActive);
    }

    private async Task SyncUserPermissionsAsync(ApplicationUser user, List<string> selectedPermissions)
    {
        var claims = await _userManager.GetClaimsAsync(user);
        foreach (var claim in claims.Where(x => x.Type == AppClaimTypes.Permission))
        {
            await _userManager.RemoveClaimAsync(user, claim);
        }

        foreach (var permission in selectedPermissions.Distinct())
        {
            await _userManager.AddClaimAsync(user, new Claim(AppClaimTypes.Permission, permission));
        }
    }

    private async Task<List<Business.Domain.Entities.Department>> GetDepartmentsAsync()
    {
        return await _context.Departments.Where(x => x.IsActive).OrderBy(x => x.Name).ToListAsync();
    }

    private async Task CleanupUserOwnedDataAsync(ApplicationUser user, CancellationToken cancellationToken)
    {
        _context.PersonalNotes.RemoveRange(_context.PersonalNotes.Where(x => x.OwnerUserId == user.Id));
        _context.PersonalTasks.RemoveRange(_context.PersonalTasks.Where(x => x.OwnerUserId == user.Id));
        _context.RecordComments.RemoveRange(_context.RecordComments.Where(x => x.CreatedByUserId == user.Id));

        var files = await _context.RecordFiles
            .Where(x => x.CreatedByUserId == user.Id)
            .ToListAsync(cancellationToken);
        foreach (var file in files)
        {
            try
            {
                var physicalPath = Path.Combine(_environment.WebRootPath, file.RelativePath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
                if (System.IO.File.Exists(physicalPath))
                {
                    System.IO.File.Delete(physicalPath);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Kullanıcı silinirken dosya kaydı fiziksel olarak temizlenemedi. FileId: {FileId}", file.Id);
            }
        }

        _context.RecordFiles.RemoveRange(files);

        var projectTasks = await _context.ProjectTasks
            .Include(x => x.Assignments)
            .Where(x =>
                x.AssignedToUserId == user.Id ||
                x.ResponsibleUserId == user.Id ||
                x.Assignments.Any(assignment => assignment.UserId == user.Id))
            .ToListAsync(cancellationToken);
        foreach (var task in projectTasks)
        {
            if (task.AssignedToUserId == user.Id)
            {
                task.AssignedToUserId = null;
            }

            if (task.ResponsibleUserId == user.Id)
            {
                task.ResponsibleUserId = null;
            }

            _context.ProjectTaskAssignments.RemoveRange(task.Assignments.Where(x => x.UserId == user.Id));
        }

        var purchaseOrders = await _context.PurchaseOrders
            .Where(x => x.RequestedByUserId == user.Id)
            .ToListAsync(cancellationToken);
        foreach (var order in purchaseOrders)
        {
            order.RequestedBy ??= user.FullName ?? user.Email ?? user.UserName;
            order.RequestedByUserId = null;
        }

        var materialRequests = await _context.MaterialRequests
            .Where(x => x.RequestedByUserId == user.Id)
            .ToListAsync(cancellationToken);
        foreach (var request in materialRequests)
        {
            request.RequestedByUserId = null;
        }

        var templateTasks = await _context.ProjectTemplateTasks
            .Where(x => x.DefaultAssignedUserId == user.Id || x.DefaultResponsibleUserId == user.Id)
            .ToListAsync(cancellationToken);
        foreach (var task in templateTasks)
        {
            if (task.DefaultAssignedUserId == user.Id)
            {
                task.DefaultAssignedUserId = null;
            }

            if (task.DefaultResponsibleUserId == user.Id)
            {
                task.DefaultResponsibleUserId = null;
            }
        }

        await _context.SaveChangesAsync(cancellationToken);
    }

    private async Task<List<UserDeletionBlockerItemViewModel>> GetUserDeletionBlockersAsync(ApplicationUser user, CancellationToken cancellationToken)
    {
        var items = new List<UserDeletionBlockerItemViewModel>();

        var taskAssignments = await _context.ProjectTasks
            .Include(x => x.Project)
            .Include(x => x.Assignments)
            .Where(x =>
                x.AssignedToUserId == user.Id ||
                x.ResponsibleUserId == user.Id ||
                x.Assignments.Any(assignment => assignment.UserId == user.Id))
            .Select(x => new
            {
                x.Id,
                x.Title,
                ProjectCode = x.Project != null ? x.Project.Code : null,
                IsAssigned = x.AssignedToUserId == user.Id,
                IsResponsible = x.ResponsibleUserId == user.Id,
                IsInAssignmentList = x.Assignments.Any(assignment => assignment.UserId == user.Id)
            })
            .ToListAsync(cancellationToken);

        items.AddRange(taskAssignments.Select(task => new UserDeletionBlockerItemViewModel
        {
            RecordId = task.Id,
            RelationType = "task",
            CanClearRelation = true,
            Category = "Görev",
            Source = "Proje görevleri",
            RecordKey = task.ProjectCode is not null ? $"{task.ProjectCode} / {task.Id}" : task.Id.ToString(),
            Title = task.Title,
            Description = BuildTaskBlockerDescription(task.IsAssigned, task.IsResponsible, task.IsInAssignmentList)
        }));

        var materialRequests = await _context.MaterialRequests
            .Include(x => x.Project)
            .Where(x => x.RequestedByUserId == user.Id)
            .Select(x => new
            {
                x.Id,
                x.RequestedItem,
                ProjectCode = x.Project != null ? x.Project.Code : null
            })
            .ToListAsync(cancellationToken);

        items.AddRange(materialRequests.Select(request => new UserDeletionBlockerItemViewModel
        {
            RecordId = request.Id,
            RelationType = "material-request",
            CanClearRelation = true,
            Category = "İhtiyaç",
            Source = "İhtiyaç listesi",
            RecordKey = request.ProjectCode is not null ? $"{request.ProjectCode} / {request.Id}" : request.Id.ToString(),
            Title = request.RequestedItem,
            Description = "Kullanıcı bu ihtiyaç kaydını oluşturan kişi olarak kayıtlı."
        }));

        var purchaseOrders = await _context.PurchaseOrders
            .Include(x => x.Project)
            .Where(x => x.RequestedByUserId == user.Id)
            .Select(x => new
            {
                x.Id,
                x.OrderNumber,
                x.Content,
                ProjectCode = x.Project != null ? x.Project.Code : null
            })
            .ToListAsync(cancellationToken);

        items.AddRange(purchaseOrders.Select(order => new UserDeletionBlockerItemViewModel
        {
            RecordId = order.Id,
            RelationType = "purchase-order",
            CanClearRelation = true,
            Category = "Sipariş",
            Source = "Satın alma siparişleri",
            RecordKey = string.IsNullOrWhiteSpace(order.OrderNumber)
                ? (order.ProjectCode is not null ? $"{order.ProjectCode} / {order.Id}" : order.Id.ToString())
                : order.OrderNumber,
            Title = order.Content,
            Description = "Kullanıcı bu siparişte siparişi veren kişi olarak kayıtlı."
        }));

        var orderedItems = items
            .OrderBy(x => x.Category)
            .ThenBy(x => x.Source)
            .ThenBy(x => x.Title)
            .ToList();

        for (var i = 0; i < orderedItems.Count; i++)
        {
            orderedItems[i].RowNumber = i + 1;
        }

        return orderedItems;
    }

    private static string BuildTaskBlockerDescription(bool isAssigned, bool isResponsible, bool isInAssignmentList)
    {
        var roles = new List<string>();
        if (isAssigned)
        {
            roles.Add("atanan kullanıcı");
        }

        if (isResponsible)
        {
            roles.Add("sorumlu kullanıcı");
        }

        if (isInAssignmentList)
        {
            roles.Add("görev atama listesi");
        }

        return $"Kullanıcı bu görevde {string.Join(", ", roles)} olarak kayıtlı.";
    }

    private void AddIdentityErrors(IdentityResult result)
    {
        foreach (var error in result.Errors)
        {
            ModelState.AddModelError(string.Empty, error.Description);
        }
    }
}
