powershell -Command "Invoke-WebRequest https://builds.bepinex.dev/projects/bepinex_be/549/BepInEx_UnityMono_x64_f2c0e0f_6.0.0-be.549.zip -OutFile temp_download.zip"
powershell Expand-Archive temp_download.zip -DestinationPath ./
del temp_download.zip
powershell -Command "Invoke-WebRequest https://github.com/Christoffyw/ZenithModding/releases/download/v0.1.0/ZenithModding.exe -OutFile ZenithModding.exe"