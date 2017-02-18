\ App demo:
include ../1991.fs

sourcedir s" public" s+ set-public-path
sourcedir s" views" s+ set-view-path

: handle-/ s" fff" ;
: handle-hi s" hi!" ;

/1991 / handle-/
/1991 /hi handle-hi

8080 1991:
