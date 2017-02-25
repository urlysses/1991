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

: handle-uid
    s" uid:" ;
: handle-uid-new
    s" uid:new" ;
: handle-uid-delete
    s" uid:delete" ;
: handle-pid
    s" pid:" ;
: handle-pid-new
    s" pid:new" ;
: handle-pid-delete
    s" pid:delete" ;
/1991 /api/v1/users/<uid> handle-uid
/1991 /api/v1/users/<uid>/new handle-uid-new
/1991 /api/v1/users/<uid>/delete handle-uid-delete
/1991 /api/v1/users/<uid>/post/<pid> handle-pid
/1991 /api/v1/users/<uid>/post/<pid>/new handle-pid-new
/1991 /api/v1/users/<uid>/post/<pid>/delete handle-pid-delete

8080 1991:
