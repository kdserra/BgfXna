cd ..

Get-ChildItem -Recurse -Directory | Where-Object { $_.Name -in 'bin', 'obj' } | Remove-Item -Recurse -Force

cd scripts
