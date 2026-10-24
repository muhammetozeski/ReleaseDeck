# Bundled sample clips

The three files under `src/ReleaseDeck.Portal/wwwroot/samples/` are original short title-card animations produced for trying the local release workflow. They are not user uploads, channel activity or successful publication evidence. The interface labels them as samples and never uploads them automatically.

`scripts/Generate-Samples.ps1` uses the already installed FFmpeg tool and a system Arial font to render 640×360 silent MP4 files at 12 frames/second. It refuses to overwrite existing assets. The sample sequence is Gather your clips, Check the details and Review the series. FFprobe confirmed actual durations of 24, 28 and 32 seconds and file sizes of 25,254, 26,907 and 28,890 bytes. A decoded overview frame was visually inspected; its title and sample label are readable.

The browser loads the real files and uses the same inspection and identity pipeline as selected user files. Portable/cloud project documents contain file identities and authored metadata, not these video bytes. No third-party footage, personal files or YouTube download is used.
