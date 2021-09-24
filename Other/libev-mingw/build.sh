#!/usr/bin/env bash
./configure --prefix="${PWD}/../../build" || exit $?

make install -j2 || exit $?
exit $?