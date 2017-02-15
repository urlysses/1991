\ 1991

include unix/socket.fs

\ User-defined routing
wordlist constant routes
: find-route ( addr u -- data )
    routes search-wordlist if
        >body @
    else 0 then ;
: register-route ( data addr u -- )
    2dup routes search-wordlist if
        routes drop nip nip
        >body !
    else
        routes get-current >r set-current      \ switch definition word lists
        nextname create ,
        r> set-current
    then ;

\ Public directory
: pubvar create 0 , 0 , ;
pubvar public
: set-public-path ( addr u -- )
    public 2! ;
: get-public-path ( -- addr u )
    public 2@ ;

\ Internal request handling
: read-request ( socket -- addr u ) pad 4096 read-socket ;

: send-response ( addr u socket -- )
    dup >r write-socket r> close-socket ;

: requested-route ( addr u -- routeaddr routeu )
    bl scan 1- swap 1+ swap 2dup bl scan swap drop - ;

: file-exists? ( addr u -- addr u bool )
    2dup file-status nip 0= ;

: serve-file ( addr u -- addr u )
    slurp-file ;

: either-resolve ( addr u -- resolveaddr resolveu )
    s" GET" search if
        requested-route
        2dup find-route dup if
                >r 2drop r>                 \ keep xt, drop the route string
                execute                     \ execute the user's route handler
            else
                drop                        \ drop the xt
                get-public-path 2swap s+    \ see if route exists in public dir
                file-exists? if
                    serve-file              \ collect file contents
                else
                    exit                    \ continue to 404
                then
            then
        s\" HTTP/1.1 200 OK\n Content-Type: text/html\n\n" 2swap s+
    rdrop exit then ;

: or-404 ( addr u -- 404addr 404u )
    2drop
    s\" HTTP/1.1 404 Not Found\n Content-Type: text/plain\n\n 404" ;

: prepare-response ( addr u -- returnaddr returnu)
    either-resolve or-404 ;

: start-server { server client }
    begin
        server 255 listen
        server accept-socket to client

        client read-request prepare-response client send-response
    again ;


\ Userland
: 1991: ( port -- )
    create-server 0 start-server ;
: /1991 ( "<path> <word>" -- )
    bl word ' swap count register-route ;


\ App demo:
: handle-/ s" fff" ;
: handle-hi s" hi!" ;

/1991 / handle-/
/1991 /hi handle-hi

8080 1991:
