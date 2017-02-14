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


\ Internal request handling
: read-request ( socket -- addr u ) pad 4096 read-socket ;

: send-response ( addr u socket -- )
    dup >R write-socket R> close-socket ;

: start-server { server client }
    begin
        server 255 listen
        server accept-socket to client

        client read-request type s\" HTTP/1.1 200 OK\n Content-Type: text/html\n\n fffff" client send-response
    again ;


\ Userland
: 1991: ( port -- )
    create-server 0 start-server ;
: 1991/ ( "<path> <word>" -- )
    bl word ' swap count register-route ;


\ App demo:
: handle-hi ." hi!" ; \ not sure printing is the way to go?

1991/ hi handle-hi

\ 8080 1991:
