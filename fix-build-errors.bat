@echo off
echo ===========================================
echo  DNA Platform - Fix Build Errors
echo ===========================================
echo.
echo Fixing syntax errors in skill files...
echo.

REM Fix ValidationSkill.cs - remove duplicate code after line 198
echo [1/4] Fixing ValidationSkill.cs...
powershell -Command "(Get-Content 'src\06_Skills\BuiltIn\ValidationSkill.cs')[0..197] | Set-Content 'src\06_Skills\BuiltIn\ValidationSkill.cs'"

REM Fix MergeResultsSkill.cs
echo [2/4] Fixing MergeResultsSkill.cs...
powershell -Command "(Get-Content 'src\06_Skills\BuiltIn\MergeResultsSkill.cs')[0..143] | Set-Content 'src\06_Skills\BuiltIn\MergeResultsSkill.cs'"

REM Fix FastaLoaderSkill.cs
echo [3/4] Fixing FastaLoaderSkill.cs...
powershell -Command "(Get-Content 'src\06_Skills\Bioinformatics\FastaLoaderSkill.cs')[0..173] | Set-Content 'src\06_Skills\Bioinformatics\FastaLoaderSkill.cs'"

REM Fix SequenceAlignerSkill.cs
echo [4/4] Fixing SequenceAlignerSkill.cs...
powershell -Command "(Get-Content 'src\06_Skills\Bioinformatics\SequenceAlignerSkill.cs')[0..119] | Set-Content 'src\06_Skills\Bioinformatics\SequenceAlignerSkill.cs'"

echo.
echo ===========================================
echo  Fix Complete!
echo ===========================================
echo.
echo Now run: build-skills.bat
echo.
pause