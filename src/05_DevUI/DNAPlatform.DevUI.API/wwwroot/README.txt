This folder must exist in Development mode.
ASP.NET Core's StaticWebAssetsLoader throws DirectoryNotFoundException on startup if wwwroot/ is missing while the static web assets manifest references it (see docs/DEVELOPMENT_LOG.md, 2026-09-06 entry).
Do not delete this folder.