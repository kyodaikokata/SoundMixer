@echo off
set SRC=E:\work\DalamudProject\WorkInProgress\SoundMixer
set DST=E:\work\DalamudProject\release\SoundMixer
robocopy "%SRC%\SoundMixer" "%DST%\SoundMixer" /E /XD bin obj /XF SoundSetter.csproj SoundSetter.json /NFL /NDL /NJH /NJS
robocopy "%SRC%\images" "%DST%\images" /E /NFL /NDL /NJH /NJS
copy /Y "%SRC%\pluginmaster.cn.json" "%DST%\pluginmaster.cn.json"
copy /Y "%SRC%\pluginmaster.global.json" "%DST%\pluginmaster.global.json"
echo synced > "%DST%\.sync-complete"
