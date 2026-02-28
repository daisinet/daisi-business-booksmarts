window.booksmarts = {
    setTheme: function (theme) {
        document.documentElement.setAttribute('data-bs-theme', theme);
    },

    openPlaidLink: function (linkToken, dotNetRef) {
        var handler = Plaid.create({
            token: linkToken,
            onSuccess: function (publicToken, metadata) {
                var institutionId = metadata.institution ? metadata.institution.institution_id : '';
                var institutionName = metadata.institution ? metadata.institution.name : '';
                dotNetRef.invokeMethodAsync('OnPlaidSuccess', publicToken, institutionId, institutionName);
            },
            onExit: function (err, metadata) {
                dotNetRef.invokeMethodAsync('OnPlaidExit');
            }
        });
        handler.open();
    }
};
