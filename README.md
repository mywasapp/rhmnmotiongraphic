# RHMN Motion Graphic Studio / RACHMANZO Stock Assets

Repository GitHub untuk proyek **RHMN Motion Graphic Studio** dan produksi animasi stok komersial **RACHMANZO**.

## Batch 02 — 12 motion graphics 4K

**Kode sumber:** [stock-batch-02](./stock-batch-02/) · **Workflow render:** [RACHMANZO Batch 02](./.github/workflows/stock-batch-02.yml).

Mesin render menggunakan Canvas 2D HTML/JS dengan `renderAtTime(t)` deterministik, Playwright Chromium, dan FFmpeg. Tidak perlu menginstal Node.js atau memakai CMD pada komputer sendiri: render dilakukan oleh GitHub Actions.

### Cara unduh MP4 setelah build berhasil

1. Buka tab **[Actions](../../actions/workflows/stock-batch-02.yml)**.
2. Pilih run **RACHMANZO Batch 02 - Render 4K MP4** terbaru.
3. Pastikan tiap job `Render part-XX` memiliki tanda centang hijau. Jika merah, lihat *job logs*; jangan unggah video yang gagal QC.
4. Pada halaman run, gulir ke bagian **Artifacts**, lalu klik berkas `RACHMANZO_Batch02_part-01` sampai `part-06` untuk mengunduh masing-masing ZIP.
5. Ekstrak ZIP untuk mendapatkan 2 video MP4 plus file metadata dan laporan QC. Dibutuhkan login GitHub untuk mengunduh artefak.

### Standar yang dicek otomatis

- Video orisinal yang dirender native **3840×2160** (horizontal), atau **2160×3840** (vertikal); bukan upscale.
- **H.264**, MP4, **30 FPS**, **300 frame**, **10 detik**, **yuv420p**, metadata warna BT.709.
- Ukuran file video **di atas 5 MiB**. Jika tidak tercapai saat CRF 15, sistem mencoba CRF 9.
- Perbandingan gambar prosedural pada waktu **t=0** dan **t=10 detik** (uji kondisi periodik pada kode).

**Penting:** ukuran di atas 5 MB dan QC otomatis bukan jaminan diterima di Adobe Stock. Tinjau setiap video untuk cacat gerakan, flicker, artefak kompresi, klaim loop, kesesuaian keyword, dan kepemilikan hak cipta. Workflow tidak mengunggah karya secara otomatis ke marketplace.

### Katalog 12 adegan

| ID | Aset |
| --- | --- |
| RMA013 | Aerospace Orbital Telemetry Rings |
| RMA014 | Isometric Logistics Cargo Flow |
| RMA015 | Clean Medical ECG Waveforms |
| RMA016 | Luxury Rose Gold Optical Folds |
| RMA017 | Solar Farm Reflective Panels |
| RMA018 | Industrial Precision Gearwork |
| RMA019 | Cloud Server Corridor Light Paths |
| RMA020 | Minimal White Architectural Shadows |
| RMA021 | Eclipse Spectral Corona |
| RMA022 | Faceted Crystal Geometry Tiles |
| RMA023 | Warm Layered Paper Cutout Waves |
| RMA024 | Vertical Aurora Light Beams |

Pilihan adegan yang telah diimplementasikan lebih beragam daripada konsep awal yang sempat didiskusikan. Nama dan metadata sesuai dengan tampilan aktual renderer, bukan klaim konsep yang belum dibuat.
