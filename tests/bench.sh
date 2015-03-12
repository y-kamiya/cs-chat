#!/bin/bash -e

scriptDir=`dirname $0`
bench=$scriptDir/../src/chatBench.exe

function calculateAverageTime()
{
    _filename=$1
    cat $_filename | grep time | awk '{n++; time += $6} END {print time/n}'
}

function executeTest()
{
    clientCount=$1
    messageCount=$2
    testCount=$3
    total=0
    logFile=log${clientCount}_${messageCount}
    for i in `seq 0 $testCount`
    do
        $bench $clientCount $messageCount > $logFile
        time=`calculateAverageTime $logFile`
        total=`echo "scale=2; $total + $time" | bc`
    done
    echo `echo "scale=2; $total / $testCount" | bc`
}

executeTest 5 100 3
executeTest 10 100 3
executeTest 15 100 3
#executeTest 64 100 3


