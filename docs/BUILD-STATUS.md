# Status verifikasi — 22 Oktober 2026

**Status: berhasil dikompilasi untuk Windows x64; belum dinyatakan selesai.**

| Pemeriksaan | Hasil |
|---|---|
| C# / .NET 10 build target Windows x64 | Lulus |
| Publish self-contained dengan warnings-as-errors | Lulus |
| Format aplikasi PE32+ GUI x86-64 | Terverifikasi |
| Komponen .NET, WebView2 loader, FFmpeg/FFprobe, contoh dan panduan | Tersedia dalam paket portable |
| Pengujian inti dan FFmpeg/FFprobe pada Linux | 31 pemeriksaan lulus |
| Pengujian logika clock JavaScript di lingkungan simulasi | 11 pemeriksaan lulus |
| Uji visual dengan Chromium lokal | Belum berhasil: browser gagal diluncurkan di lingkungan ini |
| Uji GUI WinForms dan render WebView2 pada Windows nyata | Belum dijalankan |
| Repository GitHub jarak jauh | Tersedia: `mywasapp/rhmnmotiongraphic` |
| Eksekusi GitHub Actions Windows x64 | Belum dijalankan |
| Installer Inno Setup | Konfigurasi tersedia; belum dikompilasi atau diuji |

## Apa yang benar-benar tersedia

Paket **win-x64-Preview.zip** berisi executable Windows hasil kompilasi nyata, bukan file teks yang diganti ekstensi. Paket menyertakan runtime .NET sehingga pemakai tidak perlu menginstal SDK, Node.js, atau menjalankan CMD. WebView2 Evergreen Runtime tetap diperlukan; bootstrapper Microsoft disertakan.

Keberhasilan kompilasi tidak membuktikan bahwa UI, WebView2, driver grafis, atau ekspor 4K berjalan di Windows. Paket ini diberikan untuk pemeriksaan awal, bukan klaim rilis produksi siap jual.

## Kelanjutan GitHub

Repository `mywasapp/rhmnmotiongraphic` sudah tersedia dan dapat diakses. Sumber aplikasi desktop ditambahkan berdampingan dengan renderer RACHMANZO yang sudah ada. Status Windows hanya akan diubah setelah workflow selesai dan buktinya diperiksa.

## Gate yang sudah disiapkan

Workflow Windows mengunduh komponen dengan pemeriksaan hash/signature, mengompilasi aplikasi, lalu menjalankan aplikasi asli dengan `--smoke-test`. Pengujian mencakup contoh Canvas/renderAtTime, CSS, RAF, MP4 UHD 4K, FPS pecahan, pembacaan seluruh frame, decode penuh, pemeriksaan video tidak beku dan pembatalan. Paket installer hanya dibuat setelah pengujian Windows berhasil. Hash berkas hasil pengujian harus tetap sama saat pengemasan dan setelah installer dieksekusi.

Tahap berikutnya adalah menjalankan workflow Windows, memperbaiki kegagalan bila ditemukan, lalu menyerahkan artifact yang benar-benar lulus gate tersebut.

## Bukti lokal

Folder `verification` dalam paket pratinjau memuat log build, publish, pengujian inti dan clock serta manifest SHA-256. Tidak ada URL run, nomor artifact, screenshot Windows, atau hasil pengujian Windows yang dibuat-buat.
