# AGENTS.md

## Genel Kurallar

* Bu proje ASP.NET Core MVC kullanmaktadır.
* Katmanlı mimari kullanılmaktadır.
* SQL Server kullanılmaktadır.
* Entity Framework Core kullanılmaktadır.
* ASP.NET Core Identity kullanılmaktadır.
* Yanıtlar Türkçe verilmelidir.
* Kullanıcıya görünen UI metinleri Türkçe olmalıdır.

## Kod Değişiklikleri

* Büyük refactor yapma.
* Çalışan sistemi yeniden yazma.
* Sadece istenen özelliği geliştir.
* Gereksiz dosya değişikliği yapma.
* Mevcut mimariyi koru.
* Önce mevcut kodu incele, sonra değişiklik öner.

## Identity ve Güvenlik

* ASP.NET Core Identity korunacaktır.
* Authentication yapısı korunacaktır.
* Authorization yapısı korunacaktır.
* Cookie Authentication yapısı korunacaktır.
* ASP.NET Core Data Protection sistemi korunacaktır.
* App_Data/DataProtectionKeys klasörü korunacaktır.
* Data Protection yapısını kaldırma veya değiştirme.
* Email doğrulama, şifre sıfırlama ve 2FA işlemleri mevcut Identity yapısı üzerinden geliştirilmelidir.

## Migration Politikası

* Model değişikliği gerekiyorsa migration oluştur.
* Migration gerekip gerekmediğini raporla.
* Migration ismini açıklayıcı oluştur.
* Otomatik Update-Database çalıştırma.
* Program.cs içine Database.Migrate() ekleme.
* Veritabanını otomatik değiştirme.

## Güvenlik Politikası

* Şifreler Identity tarafından hashlenmelidir.
* Gizli bilgiler source code içine yazılmamalıdır.
* SMTP şifreleri source code içine yazılmamalıdır.
* Connection string şifreleri source code içine yazılmamalıdır.
* Secret bilgiler configuration üzerinden alınmalıdır.

## Deployment Politikası

* Production ortamına özel ayarları koru.
* Mevcut publish yapısını bozma.
* web.config dosyasını gereksiz değiştirme.
* DataProtection yapısını koru.

## Görev Sonunda

Her görev sonunda aşağıdakileri raporla:

1. Değiştirilen dosyalar
2. Oluşturulan dosyalar
3. Migration gerekli mi. Update-Database gerekli mi.
4. Test adımları
5. Riskli noktalar
