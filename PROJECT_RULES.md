# PROJECT_RULES.md

## Proje Amacı

FirmaTakip, üretim yapan işletmeler için geliştirilen şirket içi operasyon yönetim sistemidir.

Amaç:

* Görev yönetimi
* Sipariş yönetimi
* Proje yönetimi
* Üretim takibi
* Tedarikçi yönetimi
* Malzeme yönetimi
* Kullanıcı yönetimi
* Dosya ve doküman yönetimi

işlemlerini tek sistem altında toplamaktır.

## Teknoloji Kararları

* ASP.NET Core MVC
* Razor View
* SQL Server
* Entity Framework Core
* ASP.NET Core Identity

Kullanılacaktır.

React, Next.js veya farklı frontend teknolojilerine geçiş planlanmamaktadır.

## Kullanıcı Rolleri

### Admin

* Kullanıcı oluşturabilir.
* Kullanıcı düzenleyebilir.
* Rol atayabilir.
* Sistemi yönetebilir.

### Çalışan

* Kendisine atanan kayıtları görüntüleyebilir.
* Yetkisi dahilindeki işlemleri yapabilir.

## Kullanıcı Kayıt Politikası

* Kullanıcılar kendi kendine kayıt olamaz.
* Kullanıcı oluşturma işlemi sadece Admin tarafından yapılır.
* Admin kullanıcı şifresi belirlemez.
* Kullanıcı davet sistemi kullanılacaktır.
* Email doğrulaması zorunludur.

## Güvenlik Politikası

* Güçlü şifre zorunludur.
* Şifremi unuttum sistemi bulunacaktır.
* Admin için Authenticator App tabanlı 2FA kullanılacaktır.
* Email doğrulaması zorunlu olacaktır.

## Dil Politikası

* Sistem dili Türkçedir.
* Kullanıcıya görünen tüm ekranlar Türkçe olacaktır.
