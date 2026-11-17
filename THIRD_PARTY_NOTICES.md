# Third-Party Notices

XIIGen's first-party code is licensed under AGPL-3.0-only; see [LICENSE](LICENSE) and [NOTICE](NOTICE). Third-party packages, copied materials and separately deployed services retain their own licenses, copyright notices and attribution requirements. XIIGen's license does not replace them.

## Dependency records

- NuGet references: project files under [flows/](flows), including [FLOW-00 infrastructure](flows/FLOW-00-bundle-activation/server/Xiigen.Flow.BundleActivation.Infrastructure/Xiigen.Flow.BundleActivation.Infrastructure.csproj), and their `packages.lock.json` files.
- Package sources and local archives: [nuget.config](nuget.config) and [local-packages/](local-packages).
- npm dependencies and resolved versions: client `package.json` and `package-lock.json` files, including [FLOW-00 manifest](flows/FLOW-00-bundle-activation/client/angular/package.json) and [lockfile](flows/FLOW-00-bundle-activation/client/angular/package-lock.json).

Project manifests and lockfiles identify dependencies and versions. Consult the exact package metadata and included license or notice files for its terms: NuGet packages can carry a license expression or license file in their metadata, and npm packages can carry a license declaration and accompanying license files. Lockfiles alone are not a complete license record. Container images and external services have their own version-specific terms.

Preserve existing notices when copying or redistributing third-party material. A contribution that adds such material should identify its source and exact version and retain its applicable license and attribution.
