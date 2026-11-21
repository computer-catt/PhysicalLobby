#!/usr/bin/env bash
dotnet publish -c Release -o ./dist 
ModFolder="$HOME/.config/r2modmanPlus-local/REPO/profiles/MEOW/BepInEx/plugins/Catburger-PhysicalLobby/"
Assetbundle="$HOME/Documents/R.E.P.O/Assets/AssetBundles/physicallobby"
cp ./dist/PhysicalLobby.dll "$ModFolder"
cp "$Assetbundle" "$ModFolder"Physical\ Lobby
exit