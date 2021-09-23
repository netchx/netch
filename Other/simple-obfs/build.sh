#!/usr/bin/env bash
git submodule update --init || exit $?

./autogen.sh || exit $?
./configure --disable-documentation --with-ev="${PWD}/libev-mingw/build" || exit $?

make -j2
exit $?