#!/usr/bin/env bash
git submodule update --init || exit $?

cd libudns
./autogen.sh || exit $?
./configure || exit $?
make -j2 || exit $?
cd ..

./autogen.sh || exit $?
CFLAGS+="-fstack-protector" ./configure --disable-documentation --with-ev="${PWD}/libev-mingw/build" || exit $?

sed -i "s/%I/%z/g" src/utils.h
sed -i "s/^const/extern const/g" src/tls.h
sed -i "s/^const/extern const/g" src/http.h

make -j2
exit $?