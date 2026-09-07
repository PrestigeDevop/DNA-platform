This directory is intentionally empty.

The DevUI frontend lives in the separate SvelteKit app (src/05_DevUI/DNAPlatform.DevUI.Web).
This folder must exist so ASP.NET Core's StaticWebAssets loader does not throw
DirectoryNotFoundException in Development mode.
