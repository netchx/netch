# Project V for SagerNet for Netch
This is not a joke.  
Modified from [SagerNet/v2ray-core](https://github.com/SagerNet/v2ray-core).  
#### Extends all features of SagerNet/v2ray-core

### Changes

- embed ShadowsocksR plugin for shadowsocks

```json
{
  "outbounds": [
    {
      "protocol": "shadowsocks",
      "settings": {
        ...
        "plugin": "shadowsocksr",
        "pluginArgs": [
          "--obfs=<OBFS_TYPE>",
          "--obfs-param=<OBFS_PARAMETERS>",
          "--protocol=<PROTOCOL_TYPE>",
          "--protocol-param=<PROTOCOL_PARAMETERS>"
        ]
      }
    }
  ]
}
```

- embed simple-obfs plugin for shadowsocks

```json
{
  "outbounds": [
    {
      "protocol": "shadowsocks",
      "settings": {
        ...
        "plugin": "obfs-local",
        "pluginOpts": "<SIMPLE_OBFS_OPTIONS>"
      }
    }
  ]
}
```

- Re-enable ReadV

### License

GPL v3

### Credits

This repo relies on the following projects:  
- [SagerNet/LibSagerNetCore](https://github.com/SagerNet/LibSagerNetCore)
