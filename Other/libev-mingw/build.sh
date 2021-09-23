#!/usr/bin/env bash
mkdir ../../build

./configure --prefix="${PWD}/../../build" || exit $?
make install -j2 || exit $?
exit $?