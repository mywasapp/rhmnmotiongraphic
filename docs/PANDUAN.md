# RHMN Motion Graphic Studio

Antarmuka bahasa Indonesia untuk mengubah proyek HTML/CSS/JavaScript menjadi video MP4 H.264.

## Mulai tanpa Node.js atau CMD

1. Ekstrak seluruh ZIP portable. Jangan menjalankan aplikasi langsung di dalam ZIP.
2. Buka `RHMN Motion Graphic Studio.exe`. .NET sudah termasuk dalam paket.
3. WebView2 Runtime harus tersedia. Jika belum, gunakan installer WebView2 resmi yang disertakan. Installer memerlukan internet; Windows 11 biasanya sudah memiliki runtime ini.
4. Klik **Buka contoh**, pilih proyek, lalu periksa preview.
5. Pilih resolusi, FPS, durasi, bitrate dan folder hasil.
6. Klik **Tambah ke antrean**, lalu **Mulai batch render**.
7. Periksa video dan laporan `.qc.txt` di folder hasil.

## Mengimpor proyek

Pilih HTML utama. CSS, JavaScript, font, dan gambar yang dirujuk harus berada di dalam folder HTML atau subfoldernya. Impor folder mengutamakan `index.html`; jika tidak ada HTML di folder utama, aplikasi mencari pada subfolder langsung. Anda juga dapat menyeret file HTML atau folder ke jendela aplikasi.

CSS dan JS bukan file video yang bisa berdiri sendiri. Keduanya harus dipanggil oleh HTML. Proyek React/Vite/Next harus diekspor menjadi situs statis terlebih dahulu. Aplikasi ini tidak menjalankan server Node.js, PHP, bundler, atau backend proyek.

Aset internet diblokir secara default. Bila proyek memakai CDN, pilih **Izinkan aset dari internet**, lalu muat ulang preview. Simpan aset di folder proyek untuk hasil yang lebih mudah diulang. Hanya buka proyek yang Anda percaya.

## Pengaturan yang disarankan

Untuk motion graphic 16:9: 3840 × 2160, 30 fps, 8–15 detik, bitrate target 60 Mbps, enkoder Seimbang. Angka bitrate adalah target VBR, bukan jaminan ukuran/bitrate tetap. Adegan sederhana dapat menghasilkan berkas lebih kecil.

Setiap antrean menyimpan salinan pengaturan saat ditambahkan. Mengubah panel pengaturan tidak mengubah pekerjaan yang sudah antre. Hapus dan tambahkan kembali pekerjaan untuk mengganti pengaturannya.

File dengan nama yang sama tidak ditimpa. Nama baru dibuat saat menambah antrean. Jika file tujuan muncul setelah antrean dibuat, pekerjaan gagal dengan pesan agar Anda memilih nama baru. Satu pekerjaan gagal tidak menghentikan pekerjaan lain. Pembatalan menghentikan pekerjaan aktif dan membiarkan pekerjaan berikutnya menunggu.

## Mode waktu

**Otomatis** memakai `window.renderAtTime(t)` jika fungsi itu tersedia. Jika tidak, aplikasi mengendalikan requestAnimationFrame, timer, Date/performance.now, dan animasi CSS/WAAPI.

Untuk hasil paling dapat diulang, siapkan fungsi `renderAtTime(t)` dengan t dalam detik. Fungsi harus menggambar keadaan lengkap untuk waktu tersebut, boleh mengembalikan Promise, dan jangan bergantung pada kecepatan komputer. Ubah ukuran internal canvas sesuai `innerWidth` dan `innerHeight`, bukan hanya ukuran CSS.

Contoh:

    window.renderAtTime = function(t) {
      canvas.width = innerWidth;
      canvas.height = innerHeight;
      // gambar seluruh adegan dari nilai t
    };

Selama ekspor `window.__RHMN_RENDERING__` bernilai true. Gunakan ini untuk menonaktifkan loop preview milik proyek bila ada renderAtTime. Semua mode memakai seed acak yang tetap.

Animasi di worker, Web Animations di shadow root tertutup, clock khusus library, video/audio/iframe, animasi GIF dan simulasi yang bergantung pada kondisi di luar halaman belum dikendalikan penuh. Audio/video/iframe ditolak saat render. Gunakan renderAtTime untuk integrasi library kompleks. CSS/RAF adalah mode kompatibilitas, bukan jaminan untuk setiap halaman web.

Preview memperlihatkan halaman pada ukuran panel. Ekspor merender viewport pada ukuran target dan memeriksa ukuran PNG setiap frame. Raster kecil tetap kecil secara detail meski output 4K; jangan menganggap ukuran output memperbaiki aset buram.

## Durasi, FPS pecahan, dan loop

Jumlah frame dibulatkan ke atas dari durasi × FPS. Untuk 29,97 atau 59,94 fps, durasi berkas dapat sedikit lebih panjang dari angka yang diminta (kurang dari satu frame). FPS pecahan menggunakan rasio tepat 30000/1001 dan 60000/1001. Untuk batas maksimum 60 detik Adobe, gunakan durasi yang sedikit lebih pendek atau FPS bulat.

Frame video ditulis mulai t=0 sampai t=(N−1)/FPS. Frame di t=N/FPS hanya dipakai untuk perbandingan loop, sehingga tidak terjadi duplikasi frame akhir. Perbandingan loop memakai sampel 64 × 64 dan merupakan indikator; kontinuitas arah/kecepatan gerak tetap harus dilihat sendiri. Contoh Aurora berulang 12 detik, Geometri CSS 8 detik, Partikel RAF 10 detik.

## QC Adobe Stock

Aturan diperiksa pada 21 Oktober 2026: durasi 5–60 detik, maksimum 3.900 MB, FPS standar, format/codec, dan ukuran standar. Aplikasi memeriksa hasil menggunakan FFprobe, lalu mendekode seluruh video dengan FFmpeg. Pemeriksaan tambahan membandingkan target resolusi, FPS dan jumlah frame dengan berkas nyata.

Laporan tidak memutuskan lisensi aset, penggunaan merek, release, status AI, atau kualitas artistik. Tinjau banding, flicker, motion, ketajaman dan hak penggunaan secara manual. Lulus teknis tidak menjamin video diterima Adobe Stock.

Sumber aturan:
https://helpx.adobe.com/stock/contributor/submit-your-content/submit-videos/technical-requirements-for-video-submissions.html

## Jika ada masalah

- **WebView2 tidak tersedia**: jalankan MicrosoftEdgeWebview2Setup.exe yang disertakan, lalu buka ulang aplikasi.
- **Aset gagal dimuat**: periksa jalur relatif CSS/JS/gambar dan pengaturan akses internet.
- **Canvas terlihat buram**: gunakan ukuran internal canvas sesuai target render.
- **Gerakan tidak benar**: pakai renderAtTime(t) dan hindari clock dari worker atau sumber eksternal.
- **Render lambat**: ini normal untuk capture PNG 4K per frame; gunakan enkoder Cepat atau uji di Full HD lebih dahulu.
- **Ruang disk tidak cukup**: pindahkan folder hasil atau kurangi durasi/bitrate.
- **Antrean lama**: status yang terputus saat aplikasi ditutup dikembalikan ke Menunggu. Ekspor dimulai kembali dari frame pertama.

Paket awal belum memiliki sertifikat penandatanganan kode. Cocokkan SHA-256 paket dengan laporan build. Jangan menonaktifkan perlindungan Windows untuk membuka file dari sumber yang tidak Anda kenal.

## Data aplikasi

Sesi otomatis dan profil WebView2 disimpan di `%LOCALAPPDATA%\RHMN\MotionStudio`. Proyek sumber tidak diubah. Video dan laporan disimpan hanya di folder yang Anda pilih.
