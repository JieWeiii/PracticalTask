# Notes

## What I'd do differently with more time
- Confirm with the customer whether TaskID needs to be unique per device. Right now shared TaskIDs are shown as a neutral note, not an error,  since neither the brief nor the schema states a rule either way.
- Add automated tests for the data-warning logic, so future changes could be checked automatically instead of manually re-running through the same test cases each time.

## What I didn't finish, or am not fully happy with
- Testing was manual throughout (running the program, editing rows in SQL, etc) reasonable given the time budget, but not something I'd want to rely on at a larger scale.

## If this was your first time with C# / .NET / SQL Server
My prior C#/.NET experience is from building websites with ASP.NET. This was my first time writing a plain console application and using ADO.NET (Microsoft.Data.SqlClient) directly for data access, without a web framework or ORM layered on top — reading configuration, opening connections, and mapping rows all had to be done by hand rather than relying on the abstractions ASP.NET usually provides.
