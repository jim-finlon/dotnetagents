# Security Policy

This policy applies to the DotNetAgents public core package surface.

## Reporting Vulnerabilities

Do not disclose suspected vulnerabilities through public issues. Until the
public coordinated-disclosure contact is published, report findings through the
maintainer contact channel used for the release candidate. The public mirror
must not be enabled until this section carries a public security contact.

## Dependency Advisory Posture

### System.Security.Cryptography.Xml Pinned To 10.0.6

The public-core `DotNetAgents.Documents` package depends on
`DocumentFormat.OpenXml` for Office file parsing. That dependency previously
flowed a transitive reference to `System.Security.Cryptography.Xml 9.0.0`,
which is affected by **GHSA-37gx-xxp4-5rgx** and **GHSA-w3x6-4m5h-cxqf**.
The patched line is `10.0.x`.

To keep the public build clean of `NU1903` advisories, `Directory.Build.props`
pins the package directly to `10.0.6` for the `DotNetAgents.Documents` project:

```xml
<PackageReference
  Include="System.Security.Cryptography.Xml"
  Version="10.0.6"
  Condition="'$(MSBuildProjectName)' == 'DotNetAgents.Documents'" />
```

The conditional limits the pin to the one project that actually uses the
transitive; every other public-core package builds unchanged.

The pin can produce a cosmetic `NU1510` not-prunable warning. That trade-off is
intentional: an advisory-free build matters more than a clean prune report on a
single package.

### Removal Criteria

Remove the pin once both of the following are true:

1. `DocumentFormat.OpenXml` ships a release whose own transitive graph resolves
   `System.Security.Cryptography.Xml` at `10.0.x` or higher.
2. A clean `dotnet restore` against `DotNetAgents.Public.sln` reports no
   `NU1903` advisory for `DotNetAgents.Documents` without the
   `Directory.Build.props` pin.

Owner: the DotNetAgents maintainer driving the public-core release.
