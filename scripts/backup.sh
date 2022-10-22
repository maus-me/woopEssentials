#!/bin/bash
# exit when any command fails
set -e


BACKUP_DIR=/home/dilli/Downloads/testing/bak
SOURCE_DIR=/home/dilli/Downloads/testing/src



# parameter1 = source dir
# parameter2 = backup dir
# has to be unqie since it will delete all filse older than a
# parameter3 = fileextension to backup (vcsbs -> vcdbs.gz)
# parameter4 = number of days to keep the backup
backupFolderVS () {
    SRC=$1
    DEST=$2
    EXT=$3
    DAYS=$4
    if [ $# -ne 4 ] ; then echo "ERROR in backupFolder: need 4 parameters, got : $@"; return ; fi
    if [ -z ${SRC} ] ; then echo "ERROR in backupFolder: SRC is unset skipping backup"; return ; fi
    if [ -z ${DEST} ]; then echo "ERROR in backupFolder: DEST is unset skipping backup"; return ; fi
    if [ -z ${EXT} ]; then echo "ERROR in backupFolder: EXT is unset skipping backup"; return ; fi
    if [ -z ${DAYS} ]; then echo "ERROR in backupFolder: DAYS is unset skipping backup"; return ; fi

    echo "backing up files: .$EXT , keep for: $DAYS days | $SRC -> $DEST"

    if [ ! -d $DEST ] ; then 
        echo "creating backup dir $DEST"
        mkdir -p $DEST
    fi

    # only backup file that havent been modified for the last 15 min
    # this is to prevent compressing files that are still in use
    FILES_TO_COMPRESS=$(find $SRC -name "*.$EXT" -mmin +15)
    for i in $FILES_TO_COMPRESS
    do
        pigz $i
        echo "Time: $(date) - $i"
    done

    # delete backuped files older x days
    # FILES_TO_REMOVE=$(find $DEST -name "*.$EXT.gz" -mtime +$4)
    # for i in $FILES_TO_REMOVE
    # do
    #     echo "removing backup " $i
    #     rm $i
    # done

    # keep first x backups and delete others
    # so we dont delte backups if the server is offline fo a few days, and does nto generate new backups
    COUNT=1
    FILES_TO_REMOVE=$(ls -t $DEST/*.$EXT.gz)
    for i in $FILES_TO_REMOVE
    do
        if [ $COUNT -gt $DAYS ] ; then
            echo "removing file $i"
            rm $i
        fi
        COUNT=$(($COUNT + 1))
    done

    # backup only files that are ext.gz
    FILES_TO_BACKUP=$(find $SRC -name "*.$EXT.gz")
    for i in $FILES_TO_BACKUP
    do
        echo "Time: $(date) - backing up " $i
        rsync --bwlimit=20000 --remove-source-files $i $DEST
        echo "Time: $(date) - file done "
    done
    echo "Time: $(date) done -------------- " $DEST
}

echo "Time: $(date) --- backup started"
backupFolderVS $SOURCE_DIR $BACKUP_DIR "vcdbs" 5
echo "Time: $(date) --- backup finished"
