#!/usr/bin/env bash
mkdir -p "${PWD}/../../build"

./configure --prefix="${PWD}/../../build" || exit $?

make install -j2 || exit $?
exit $?