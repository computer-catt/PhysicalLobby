#!/usr/bin/env bash
ModFolder="$HOME/.config/r2modmanPlus-local/REPO/profiles/MEOW/BepInEx/plugins/Catburger-PhysicalLobby/"
Assetbundle="$HOME/Documents/R.E.P.O/Assets/AssetBundles/physicallobby"
cp "$Assetbundle" "$ModFolder"Physical\ Lobby
dotnet publish -c Release -o ./dist 
cp ./dist/PhysicalLobby.dll "$ModFolder"
exit