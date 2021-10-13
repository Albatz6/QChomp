// Please see documentation at https://docs.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.

//Represents game difficulty
// 0 - easy, 1 - medium, 2 - hard
var difficulty = 0;
var userMove = true;


// Handle difficulty radio button group switching
var selector = document.getElementById('difficulty-selection');
selector.addEventListener('click', ({ target }) => {
    if (target.getAttribute('name') === 'diff-radio') {
        difficulty = parseInt(target.value);
        console.log("Difficulty:", difficulty);

        // TODO: reset game board and switch model
    }
});

// Create grid and handle it
var lastClicked;
var grid = clickableGrid(6, 9, function (el, row, col) {
    console.log("You clicked on element:", el);
    console.log("You clicked on row:", row);
    console.log("You clicked on col:", col);

    el.className = 'clicked';
    if (lastClicked) lastClicked.className = '';
    lastClicked = el;

    if (userMove) {
        userMove = false;
        var move = makeMove(row, col);
    }
});

document.body.appendChild(grid);

// Grid forming function
function clickableGrid(rows, cols, callback) {
    var grid = document.createElement('table');
    grid.className = 'grid';

    for (var r = 0; r < rows; ++r) {
        var tr = grid.appendChild(document.createElement('tr'));
        tr.style.height = '35px';

        for (var c = 0; c < cols; ++c) {
            var cell = tr.appendChild(document.createElement('td'));
            cell.id = `cell-${r * cols + c}`;
            cell.style.width = '35px';

            if (r === 0 && c === 0) cell.bgColor = 'red';

            cell.addEventListener('click', (function (el, r, c) {
                return function () {
                    callback(el, r, c);
                }
            })(cell, r, c), false);
        }
    }

    return grid;
}

// AJAX request that sends the user move and returns AI's
function makeMove(row, col) {
    var move = []
    var token = document.getElementById('RequestVerificationToken').value;

    fetch('/?handler=Action', {
        method: 'POST',
        headers: {
            "RequestVerificationToken": token,
            "Content-Type": "application/json"
        },
        body: JSON.stringify({
            x: row,
            y: col
        })
    })
        .then(response => response.json)
        .then(result => {
            console.log(result);
            // TODO: retrieve AI's move coords from response
        }) 

    return move;
}
