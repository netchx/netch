#!/usr/bin/env bash
git submodule update --init || exit $?

./autogen.sh || exit $?
./configure --disable-documentation --with-ev="${PWD}/../../build" || exit $?

make -j2 || exit $?

gcc $(find src/ -name "obfs_local-*.o") $(find . -name "*.a" ! -name "*.dll.a") "${PWD}/../../build/lib/libev.a" -o obfs-local -fstack-protector -static -lws2_32 -s
exit $?