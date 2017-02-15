\ App demo:
include ~+/1991.fs

s" ~+/examples/public" set-public-path

: handle-/ s" fff" ;
: handle-hi s" hi!" ;

/1991 / handle-/
/1991 /hi handle-hi

8080 1991:
