:: Download and unzip BepInEx 6.*
powershell -Command "Invoke-WebRequest https://builds.bepinex.dev/projects/bepinex_be/549/BepInEx_UnityIL2CPP_x64_f2c0e0f_6.0.0-be.549.zip -OutFile temp_download.zip"
powershell Expand-Archive temp_download.zip -DestinationPath ./
del temp_download.zip

:: Get basic Zenith modding tool
powershell -Command "Invoke-WebRequest https://github.com/Christoffyw/ZenithModding/releases/latest/download/ZenithModding.exe -OutFile ZenithModding.exe"
