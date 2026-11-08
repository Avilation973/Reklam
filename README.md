# Pusula PC Bakım (C#)

Windows masaüstü GUI bakım aracı, .NET Framework WinForms ile C# dilinde yazılmıştır.

## Özellikler
- 120 Windows ayarı ve yönetim aracını kategori ve arama ile açma.
- İşletim sistemi, boş sistem diski alanı ve kullanılabilir RAM özeti.
- Yalnızca kullanıcı geçici klasöründe 7 günden eski dosyaları tarayıp kullanıcı onayıyla temizleme.
- DNS önbelleğini onayla yenileme.
- Türkçe, koyu temalı Windows GUI.

Uygulama kendiliğinden yönetici izni istemez. Sistem dosyalarını veya kayıt defterini otomatik değiştirmez. Bazı ayar bağlantıları Windows sürümüne göre bulunmayabilir.

## Derleme
Visual Studio Developer Command Prompt veya .NET Framework C# Compiler ile:

    csc /target:winexe /optimize+ /out:PusulaPcBakim.exe /reference:System.Windows.Forms.dll /reference:System.Drawing.dll PusulaPcBakim.cs


