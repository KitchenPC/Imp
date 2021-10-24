var KitchenPC = (function () {
    function doPost(uri, payload, success, fail) {
        var ticket = KPC.Security.GetTicket();

        $.ajax({
            url: '/api/' + uri,
            type: 'post',
            contentType: "application/json; charset=utf-8",
            data: JSON.stringify(payload),
            headers: (ticket ? { Authorization: 'Bearer ' + ticket } : null),
            dataType: 'json',
            success: success
        });
    }

    function doGet(uri, success, fail) {
        var ticket = KPC.Security.GetTicket();
        
        $.get({
            url: '/api/' + uri,
            headers: (ticket ? { Authorization: 'Bearer ' + ticket } : null),
            dataType: 'json',
            success: success
        });
    }

    var proxy = {
/*[PROXY_FUNCTIONS]*/
    }

    return proxy;
})();