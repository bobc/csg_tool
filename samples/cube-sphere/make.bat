@echo off

path %path%;"c:\Program Files\OpenSCAD"

for %%f in (*.scad) do openscad -o %%~nf.csg %%f
