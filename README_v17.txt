BetKookBridge v17
------------------
发布两种方式：
1) VS 右键“发布”→ 选择 DesktopProfile（输出到 桌面\BetKookBridge_v17\，单 EXE）。
2) 命令行：
   dotnet publish ".\BetKookBridge.csproj" -c Release -r win-x64 -p:SelfContained=true -p:PublishSingleFile=true -p:EnableCompressionInSingleFile=true -p:PublishTrimmed=false -p:DebugType=none -o ".\publish_one"

注意：发布前请关闭正在运行的旧 EXE，避免“文件被占用”。
