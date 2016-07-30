PowerShell -Command "Set-ExecutionPolicy Bypass -force"
SET StorageName=vanteststorage
SET StorageKey=storage primary/secondary key
net user %StorageName% /delete
net user %StorageName% %StorageKey% /add /Y
cmdkey /add:vanteststorage.file.core.windows.net /user:%StorageName% /pass:%StorageKey%
net use P: \\vanteststorage.file.core.windows.net\testshare /user:%StorageName% %StorageKey%