#!/bin/bash
cd "$(dirname "$0")"
title "";
echo off;
clear&&clear
dotnet publish -c Release --framework net5.0 --runtime osx-x64 