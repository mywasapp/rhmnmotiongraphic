# Status build — 22 Oktober 2026

**Versi 0.1.0 berhasil dikompilasi, dirender, dan dipasang pada Windows x64.**

- [Workflow Windows yang lulus](https://github.com/mywasapp/rhmnmotiongraphic/actions/runs/39684684263)
- Commit kode terverifikasi: `82d2dbe9ecb81eb57b0e3df88a058f46161c8e9b`.
- 31 pengujian inti/FFmpeg dan 11 pengujian logika clock lulus.
- 5 kasus integrasi Windows lulus: Canvas 1080p, CSS 29,97 FPS, RAF UHD 4K, pembatalan, serta jendela utama/impor/preview/antrean.
- Masing-masing video uji berisi 150 frame; seluruh frame didekode dan video diperiksa agar tidak beku.
- Installer Inno Setup berhasil dikompilasi dan dipasang. Isi terpasang cocok dengan payload yang diuji.
- Paket unduhan telah diperiksa kembali: 297 berkas cocok dengan manifest dan 294 berkas cocok dengan hasil pengujian Windows.

Artifact `RHMN-Motion-Graphic-Studio-win-x64` pada run di atas berisi installer, portable ZIP, SHA256SUMS, dan manifest. Artifact `Windows-test-evidence` berisi video uji, screenshot, dan laporan. Artifact GitHub memiliki masa simpan; pengguna dapat menyimpan paket yang sudah diunduh.

Satu pengujian antarmuka sebelumnya gagal karena pemeriksaan preview mendahului event Shown. Pengujian telah disinkronkan dengan lifecycle jendela dan workflow lengkap berhasil diulang. Keberhasilan ini melampaui cross-compilation pada paket Preview awal.

## Batas cakupan

Pengujian dilakukan pada `windows-2025` / X64. Baca [Panduan](PANDUAN.md) untuk dukungan proyek dan keterbatasan renderer. QC Adobe Stock merupakan pemeriksaan teknis; peninjauan visual, hak penggunaan dan keputusan Adobe tetap diperlukan. Installer belum ditandatangani dengan sertifikat penerbit.
