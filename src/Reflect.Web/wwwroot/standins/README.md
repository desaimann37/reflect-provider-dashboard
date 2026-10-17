# Stand-in front-end (delete on merge)

These files imitate pieces of the real Reflect XR web app so the Provider extension can run on its own:

- `css/theme-standin.css`: brand tokens (exact names and values from the tech spec), IBM Plex `@font-face`, and lookalikes of the real theme's utility classes (`card custom-card`, `avatar`, `bg-*-transparent`, `btn-primary-light`, `fs-10` to `fs-30`).
- `css/layout-standin.css`: sidebar, header, breadcrumb, accessibility panel, loading overlay.
- `js/theme-init.js`: applies the saved theme, text size, color-blind mode and sidebar state before first paint.
- `js/layout-standin.js`: theme toggle, sidebar collapse / off-canvas, accessibility panel.
- `js/shared-services-standin.js`: `MessageService`, `displayLoading` / `hideDisplayLoading`, `renderBadge`, confirm-before-submit, required-field stars, `table.dt-client`, `initTabDataTableFix`, `initTabPersistence`, and the small declarative behaviors, with the same names as the real globals.
- `img/`: Reflect XR logos from the official brand package (used only by the stand-in shell).
