window.onload = function () {
    var graphQLVue = new Vue({
        el: '#editor',
        data: {
            message: '{movies{title,year,director,cast{name,character}}}',
            graphQLJson: ''
        },
        methods: {
            fetchJson: function () {
                fetch('http://localhost:8080/api/graphql?query=' + this.message)
                    .then(response => response.json())
                    .then(data => this.graphQLJson = JSON.stringify(data, undefined, 2))
                    .catch(function (error) {
                        console.log('Cannot reach the API. ' + error);
                    });
            }
        },
        created: function () {
            this.fetchJson();
        },
        watch: {
            message: function () {
                this.fetchJson();
            }
        }
    })
};
