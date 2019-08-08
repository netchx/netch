#!/usr/bin/env python3
# -*- coding: utf-8 -*-
import hashlib, os

text = '''| 文件名 | SHA256 |
| :- | :- |'''

def checksum(filename):
    algorithm = hashlib.sha256()
    with open(filename, 'rb') as f:
        for byte_block in iter(lambda: f.read(4096), b''):
            algorithm.update(byte_block)
    return str(algorithm.hexdigest())


def filelist(path):
    r = []
    n = os.listdir(path)
    for f in n:
        if not os.path.isdir(f):
            r.append(f)
    return r

r = filelist('release')

print(text)
for i in r:
    print('| {0} | {1} |'.format(i, checksum('release\\' + i)))