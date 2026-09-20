# AI_NOTES

## 1. What I asked the AI to do
1. Walk through the task brief with me and confirm I understood the requirements correctly, especially the deliberately messy sample data.
2. Help me set up my local environment — installing SQL Server Express, the VS Code mssql extension, and confirming the .NET SDK. 
3. Generate an initial version of the C# console application covering the four requirements in the brief.
4. Help me test that version against the real sample data, debug the issues that came up, and update the code based on what I found.

## 2. One thing it got wrong, or did badly — and how I noticed
Reading through the generated code before testing it, I noticed the data-quality checks only handled problems within a single row, with nothing comparing across rows. Looking at the sample data myself, I noticed EquipId 8 and 9 both had TaskID = 8, which wasn't flagged anywhere. I also questioned whether the Enable column could safely be treated as a plain 0/1 boolean, since the table definition has no CHECK constraint limiting it to those two values (the original code silently treated any non-zero value as "enabled"), which could hide genuinely bad data instead of flagging it. I later cross-checked every nullable column against the warning logic and found gaps: Port and Version had no NULL check at all, and SlotIndex/ScanRate only checked for negative/zero, not NULL. The sample data never exposed this, since none of those columns happen to be NULL in it.

## 3. What I changed myself
1. Added a cross-row check for shared TaskIDs, but had it shown as a neutral note rather than an error (the brief never states TaskID must be unique, so treating it as a hard error would mean guessing at a business rule the customer never set.)
2. Had the Enable field changed to keep the raw database value separate from the computed true/false value, so a value that is neither 0 nor 1 gets flagged explicitly instead of silently being read as "enabled".
3. Added the missing NULL checks for Port, Version, SlotIndex, and ScanRate.

All changes came from comparing the code against the actual sample data myself, not from something the AI flagged on its own.
