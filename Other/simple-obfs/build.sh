#!/usr/bin/env bash
git submodule update --init || exit $?

./autogen.sh || exit $?
./configure || exit $?

make -j2
exit $?