# Security posture — DotNetAgents public-core staging

This staging snapshot is the audited candidate for the future public
`dna-sync/dotnetagents` repository. Anything documented here applies to the
public-core package surface, not to the private DNA monorepo.

## Reporting vulnerabilities

Until the public repository is live, report security findings to the DNA
maintainers through the private DNA workspace channels. After the public push,
this section will be replaced with the public coordinated-disclosure address.

## Dependency advisory posture

### System.Security.Cryptography.Xml — pinned to 10.0.6 for DotNetAgents.Documents

The public-core `DotNetAgents.Documents` package depends on
`DocumentFormat.OpenXml` for Office file parsing. That dependency previously
flowed a transitive reference to `System.Security.Cryptography.Xml 9.0.0`,
which is affected by **GHSA-37gx-xxp4-5rgx** and **GHSA-w3x6-4m5h-cxqf** (XML
signature spoofing / arbitrary code execution). The patched line is
`10.0.x`.

To keep the public build clean of `NU1903` advisories,
`staging/public-dotnetagents/Directory.Build.props` pins the package directly
to `10.0.6` for the `DotNetAgents.Documents` project:

```xml
<PackageReference
  Include="System.Security.Cryptography.Xml"
  Version="10.0.6"
  Condition="'$(MSBuildProjectName)' == 'DotNetAgents.Documents'" />
```

This mirrors the private `DNA/Directory.Build.props` pin. The conditional
limits the pin to the one project that actually uses the transitive — every
other public-core package builds unchanged.

The pin produces a cosmetic `NU1510` "not prunable" warning. That trade-off
is intentional: an advisory-free build matters more than a clean prune
report on a single package.

### Removal criteria

Remove the pin once **all** of the following are true:

1. `DocumentFormat.OpenXml` ships a release whose own transitive graph
   resolves `System.Security.Cryptography.Xml` at `10.0.x` or higher.
2. A clean `dotnet restore` against the staging `DotNetAgents.Public.sln`
   reports no `NU1903` advisory for `DotNetAgents.Documents` without the
   `Directory.Build.props` pin.
3. The matching private `DNA/Directory.Build.props` pin is removed in the
   same change so private and public surfaces stay in lockstep.

### Owner

Tracking issue: SDLC story `c845ee14` (Security: resolve public-core
`System.Security.Cryptography.Xml` vulnerability warnings).

Owner: the DNA maintainer driving the open-core release; rotate as the
public-core release owner rotates.

Next review: when `DocumentFormat.OpenXml` 3.6.x or 4.x ships, or earlier
if NIST/GitHub publishes a new advisory in the same package family.
