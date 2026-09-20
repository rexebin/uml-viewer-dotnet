# UML Viewer .NET

Scans a single .NET project and produces a structural inventory of its types, as a foundation for an eventual solution-wide UML viewer.

## Language

**Structural model**:
The JSON output of a scan: the set of discovered types and files for one project. Describes shape only — no relationships between types.
_Avoid_: schema, output, result

**Type entry**:
One record in the structural model describing a single class, interface, record, struct, or enum — including merged partial declarations.
_Avoid_: node, symbol

**Merged partial type**:
A type declared with `partial` across multiple files, represented as a single type entry whose `sourceFiles` lists every file it's declared in.
_Avoid_: partial class (too narrow — applies to interfaces, structs, records too)

**Containing type**:
The type entry that lexically encloses a nested type. A top-level type has no containing type.
_Avoid_: parent, outer type

**Association**:
A reference from one type to another via a field, property, or parameter type. Deliberately out of scope for the current MVP.
_Avoid_: dependency, relationship (too broad — this project also uses "dependency" for project-to-project references, a distinct future concept)
