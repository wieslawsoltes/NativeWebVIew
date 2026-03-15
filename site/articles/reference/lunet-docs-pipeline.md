---
title: "Lunet Docs Pipeline"
---

# Lunet Docs Pipeline

This repository uses Lunet for authored documentation and generated .NET API reference.

## Site structure

- `site/config.scriban`: Lunet config, project metadata, base path, and `api.dotnet` setup
- `site/menu.yml`: top-level navigation
- `site/readme.md`: landing page
- `site/articles/**`: authored documentation pages
- `site/articles/**/menu.yml`: section sidebars
- `site/images/**`: site assets
- `site/.lunet/css/template-main.css`: precompiled template stylesheet (runtime Sass workaround)
- `site/.lunet/css/site-overrides.css`: project-specific styling
- `site/.lunet/includes/_builtins/bundle.sbn-html`: bundle override used by the custom lite bundle
- `site/.lunet/layouts/**`: API layout overrides

## API generation

The API reference is generated from the shipped projects under `src/` via `with api.dotnet` in `config.scriban`.

Current API settings:

- `TargetFramework: net8.0`
- local API pages generated under `/api`
- package id advertised by the docs site: `NativeWebView`
- `external_apis` mappings for Avalonia assemblies to `https://api-docs.avaloniaui.net/docs`

## Styling and publish path

Lunet `1.0.10` on macOS 15 has a Dart Sass platform detection issue.
To keep the full template visual quality:

- docs pages are assigned `bundle: "lite"` via `with attributes`
- a local `/_builtins/bundle.sbn-html` override resolves bundle links safely
- `template-main.css` is precompiled and committed, then loaded by the `lite` bundle

The production site is published under `/NativeWebView`, so `config.scriban` must keep `site_project_basepath` aligned with the GitHub repository name.

## Commands

From repository root:

```bash
./build-docs.sh
./check-docs.sh
./serve-docs.sh
```

`build-docs.sh`/`build-docs.ps1` clean stale Lunet output and cached `NativeWebView*.api.json` artifacts before rebuilding, so API/docs output always comes from the current source tree instead of prior build residue.

`check-docs.sh`/`check-docs.ps1` build the site and then verify:

- required landing, section, and reference routes exist
- generated output contains no raw `.md` links or `/readme` routes
- reference pages do not point at the stale `api/index.md` path
- landing and article pages do not contain legacy GitHub or Pages URLs from before the rename
- generated asset links do not use the legacy production base path from before the rename
- the footer uses the project MIT license instead of the template Creative Commons footer
- the generated `NativeWebView` API page keeps its external Avalonia type links
- production pages use the `/NativeWebView`-prefixed asset paths

`serve-docs.sh` and `serve-docs.ps1` print the local URL, auto-select the next free port when `DOCS_PORT` is already in use, and start from a clean docs output directory before entering watch mode.

PowerShell:

```powershell
./build-docs.ps1
./check-docs.ps1
./serve-docs.ps1
```

GitHub Actions use `check-docs.sh` both for PR validation and for the publish workflow, so broken docs links/routes fail before deployment.

All commands run Lunet in `site/` and output to `site/.lunet/build/www`.
