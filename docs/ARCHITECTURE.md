# Architecture and test boundaries

`Rhmn.Core` is a platform-independent .NET library for validated settings, exact rational frame rates, safe filenames, queue persistence, argument-list process invocation, FFprobe parsing and Adobe technical checks.

`Rhmn.Studio` is a WinForms application. Native controls handle files and queues; WebView2 is reserved for the imported project preview/rendering. Each render creates a fresh browser control and unique virtual host mapped to the HTML directory. Scripts cannot call native host objects; web messages, permissions, downloads and new windows are denied. Navigation stays on the project origin. Internet assets require explicit UI opt-in. The normal Chromium sandbox is not disabled.

The render clock is injected before document scripts. It supplies a deterministic Date/performance clock and seeded randomness, intercepts RAF and timers, then awaits the requested scene and a native paint boundary. CSS/WAAPI animation times are set for each output frame. renderAtTime takes precedence in Auto. A capture is rejected if the encoded PNG dimensions disagree with the export settings.

Frames are piped as PNG to FFmpeg; no full frame sequence is written to disk. Backpressure is awaited. FFmpeg output goes to a unique partial path. The final name is published only after encoder exit, FFprobe frame-count/dimension/FPS verification and a complete decode pass. Cancellation kills the encoder process tree and removes its partial output. Existing final outputs are never overwritten.

The H.264 bitrate is VBR. The RGB input is converted to limited-range BT.709 YUV420p; the MP4 stream carries matching metadata. Frame count uses ceiling(duration × rational FPS), so fractional-rate exports can exceed requested duration by less than one frame. The optional loop endpoint is captured separately and is never appended to the encoded video.

## Verification layers

1. Core tests on the host: validation boundaries, fractional timing, QC failures, filename collisions, persistence, actual FFmpeg/FFprobe sample encode.
2. Cross-compile/publish: real Windows PE apphost and self-contained runtime files. Does not establish Windows runtime correctness.
3. Windows smoke test: execute published app with `--smoke-test <folder>`, create the actual WebView2, render HTML/Canvas/CSS/RAF fixtures at HD and UHD, check output and cancellation cleanup. Nonzero exit blocks package publication.
4. Manual UI acceptance on user's Windows machine: DPI scaling, display drivers, representative production projects, long 4K runs, cancellation and recovery. This cannot be replaced by compilation.

## Known boundaries

No embedded media/iframe support; no worker clock virtualization; no closed-shadow-root animation traversal; no guarantee for browser plugins or external nondeterminism. Live preview has the panel viewport rather than output viewport. Canvas bitmap resolution must be set by the source project. Source HTML is not modified. Arbitrary project JavaScript is executed in WebView2; users should import trusted projects.

The HTTP resource filter is not an OS network isolation boundary (for example, worker/WebSocket traffic may have different interception behavior). Import trusted source projects; do not use the application as an untrusted-code analysis sandbox.

Rules source and review date are stored in each report. Automated checks do not detect brand/IP violations or guarantee a subjective stock review result. Visual quality, loop velocity, flicker and banding remain manual checks.
