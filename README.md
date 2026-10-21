# RHMN Motion Graphic Studio

Windows x64 desktop application in **C# / .NET 10 + WinForms + WebView2 + FFmpeg**, with an Indonesian interface.

## Verification status

See `docs/BUILD-STATUS.md` for the actual verified state. A workflow file, source ZIP or a successful cross-compilation is not proof that the application has passed Windows integration testing. Release packaging is gated on the Windows smoke test.

## Features

- Import HTML files/folders and local CSS/JS/assets; live preview.
- Per-job settings, persistent batch queue, progress, estimated remaining time, cancellation, collision-safe MP4 output.
- 1080p, UHD 4K, DCI 4K, vertical and square presets; exact fractional FPS; duration and bitrate controls.
- Deterministic renderAtTime(t) integration and compatibility clock for CSS/RAF/timers.
- PNG frame streaming into FFmpeg/libx264, yuv420p, BT.709 tagging, faststart MP4.
- FFprobe verification, complete decode validation, loop endpoint comparison and Indonesian QC reports.
- Remote HTTP assets blocked by default; no native host objects exposed to project JavaScript.
- Three original example projects and Indonesian user guide.

No Node.js, developer SDK or terminal is needed by the end user. The portable package includes .NET and FFmpeg. WebView2 Evergreen Runtime is a prerequisite; its bootstrapper is included in release packages.

## Development

Install .NET 10 SDK. On Windows:

```powershell
./scripts/Get-Dependencies.ps1
dotnet run --project tests/Rhmn.Tests -c Release
dotnet publish src/Rhmn.Studio -c Release -r win-x64 --self-contained true -o artifacts/portable
./scripts/Test-Windows.ps1 -AppDirectory artifacts/portable
./scripts/Package.ps1
```

Cross-compilation is supported with `EnableWindowsTargeting=true`. Running the WinForms/WebView2 app requires Windows. Tests in `Rhmn.Tests` are a dependency-free assertion executable and return a nonzero exit code on failure. Set `RHMN_TEST_FFMPEG` and `RHMN_TEST_FFPROBE` to run an actual FFmpeg integration test.

## GitHub Actions

`.github/workflows/windows-x64.yml` runs on push, pull request and manual dispatch. It builds self-contained x64 output, runs core tests, launches the real WebView2 app to export three videos (including 4K and fractional FPS), verifies them, and only then packages portable and installer artifacts. Failure logs and smoke results are uploaded even on failure. A failed smoke test does not publish a usable release artifact.

Repository: [mywasapp/rhmnmotiongraphic](https://github.com/mywasapp/rhmnmotiongraphic). The desktop application coexists with the existing RACHMANZO batch renderer. The workflow requires GitHub Actions to be enabled and Windows runner minutes to be available. No PAT or credential is stored in the source.

## Scope and limitations

See [Panduan](docs/PANDUAN.md) and [Architecture](docs/ARCHITECTURE.md). This is a web-animation exporter, not a general browser screen recorder, video editor or arbitrary Node application converter. Embedded media and iframes are not supported. Complex animation engines should implement renderAtTime(t). QC is a technical preflight, not Adobe approval.

## Third-party components

See `THIRD-PARTY-NOTICES.md`. FFmpeg is invoked as a separate executable. Keep its notices, license and source information with redistributed packages. The application source is provided to the project owner; no public open-source license is assumed.

---

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
