#!/usr/bin/env bash
dotnet publish -c Release -o ./dist 
ModFolder="$HOME/.config/r2modmanPlus-local/REPO/profiles/MEOW/BepInEx/plugins/Catburger-PhysicalLobby/"
cp ./dist/PhysicalLobby.dll "$ModFolder"
# cp ./UnityProject/Assets/AssetBundles/skeeg $ModFolder
exit