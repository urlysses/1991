\ App demo:
include ../1991.fs

sourcedir s" public" s+ set-public-path
sourcedir s" views/" s+ set-view-path

: handle-/ s" fff" ;
: handle-hi s" hi!" ;

\ Basic routing:
/1991 / handle-/
/1991 /hi handle-hi

\ Views:
: page-title s" hmmmm" ;
: handle-/index
    s" index.html" render-view ;
: handle-import
    s" import-version.html" render-view ;
/1991 /index handle-/index
/1991 /import handle-import

8080 1991:
