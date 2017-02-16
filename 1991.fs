\ 1991

include unix/socket.fs

\ Helper words
: +s ( addr1 u1 addr2 u2 -- addr3 u3 )          \ like s+ but prepend rather than append.
    2swap s+ ;
: exchange ( a1 a2 -- )
    2dup c@ swap c@ rot c! swap c! ;
: reverse ( caddr u -- )                        \ reverse a string
    1- bounds begin 2dup > while
        2dup exchange
    -1 /string
    repeat 2drop ;

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
        routes get-current >r set-current       \ switch definition word lists
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

\ Request's Content-Type
pubvar RequestContentType
: set-content-type ( addr u -- )
    RequestContentType 2! ;
: get-content-type ( -- addr u )
    RequestContentType 2@ ;

: ctype? ( addr u -- )
    s" html" compare 0= if
        s" text/html"
    else
        s" text/plain"
    then ;

\ Internal request handling
: HTTP/1.1 s" HTTP/1.1 " ;

: response-status ( u -- addr u )
    dup case                                    \ get status code info
        200 of s"  OK" endof
        404 of s"  Not Found" endof
    endcase
    s\" \n" s+
    rot s>d <# #s #> +s                         \ convert status code to string and prepend to info
    HTTP/1.1 +s ;                               \ prepend HTTP/1.1

: content-type ( addr u -- caddr cu )
    s" Content-Type: " +s                       \ Prepend to the provided content type
    s\" \n\n" s+ ;                              \ Append 2 new lines

: set-header ( u addr u -- raddr ru )           \ Accepts status code and content type string
    rot response-status                         \ Set response status
    2swap content-type                          \ Set content-type
    s+ ;                                        \ Join

: read-request ( socket -- addr u )
    pad 4096 read-socket ;

: send-response ( addr u socket -- )
    dup >r write-socket r> close-socket ;

: requested-route ( addr u -- routeaddr routeu )
    bl scan 1- swap 1+ swap 2dup bl scan swap drop - ;

: file-exists? ( addr u -- addr u bool )
    2dup file-status nip 0= ;

: .extension ( addr u -- addr u )
    2dup reverse                                \ reverse the file name
    2dup s" ." search                           \ search for the first occurance of "."
    if
        swap drop -                             \ remove the "." from the search results
    else
        s" txt"
    then
    2dup reverse ;                              \ reverse reversed extension

: serve-file-type ( addr u -- )
    .extension ctype? set-content-type ;

: serve-file ( addr u -- addr u )
    slurp-file ;

: 404content-type s" txt" ctype? ;
: 404html s" 404";

: either-resolve ( addr u -- resolveaddr resolveu )
    s" GET" search if
        s" html" ctype? set-content-type        \ reset the request's content-type
        requested-route
        2dup find-route dup if
                >r 2drop r>                     \ keep xt, drop the route string
                execute                         \ execute the user's route handler
            else
                drop                            \ drop the null xt
                get-public-path +s              \ see if route exists in public dir
                file-exists? if
                    2dup serve-file             \ collect file contents
                    2swap serve-file-type       \ set the file type
                else
                    exit                        \ continue to 404
                then
            then
        200 get-content-type set-header +s
        rdrop exit then ;

: or-404 ( addr u -- 404addr 404u )
    2drop
    404 404content-type set-header 404html s+ ;

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
