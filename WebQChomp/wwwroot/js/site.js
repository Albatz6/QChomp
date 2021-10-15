// Please see documentation at https://docs.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.

//Represents game difficulty
// 0 - easy, 1 - medium, 2 - hard
var difficulty = 0;
var userMove = true;
var winner = 0;


// Handle difficulty radio button group switching
/*var selector = document.getElementById('difficulty-selection');
selector.addEventListener('click', ({ target }) => {
    if (target.getAttribute('name') === 'diff-radio') {
        difficulty = parseInt(target.value);
        console.log("Difficulty:", difficulty);

        // TODO: reset game board and switch model
    }
});*/

// Create grid and handle it
var lastClicked;
var grid = clickableGrid(6, 9, async function (el, row, col) {
    console.log("You clicked on element:", el);
    console.log("You clicked on row:", row);
    console.log("You clicked on col:", col);

    //Paint chosen area
    function paint(x, y) {
        for (var r = x; r < 6; ++r) {
            for (var c = y; c < 9; ++c) {
                var cell = document.getElementById(`cell-${r * 9 + c}`);
                if (cell.bgColor !== 'blue') cell.bgColor = 'blue';
            }
        }
    }

    // Handle user move
    if (userMove && el.bgColor !== 'blue') {
        paint(row, col);

        // Let AI make it's move
        userMove = false;
        var move = await makeMove(row, col);
        if (move[0] != -1 && move[1] != -1) {
            paint(move[0], move[1]);
        }
        console.log("AI move:", move);

        // 0 - game continues, 1 - user won, 2 - AI won
        switch (move[2]) {
            case 1:
                document.getElementById("game-info").innerHTML = "You've won!";
                userMove = false;
                break;
            case 2:
                document.getElementById("game-info").innerHTML = "AI won!";
                userMove = false;
                break;
            default:
                userMove = true;
                break;
        }
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
async function makeMove(row, col) {
    var move = []
    var token = document.getElementById('RequestVerificationToken').value;

    const data = await fetch('/?handler=Action', {
        method: 'POST',
        headers: {
            "RequestVerificationToken": token,
            "Content-Type": "application/json"
        },
        body: JSON.stringify({
            x: row,
            y: col,
            reset: false
        })
    })
        .then(response => response.json());

    move.push(data.height);
    move.push(data.width);
    move.push(data.winner);

    return move;
}

// Resetting game grid
async function reset() {
    userMove = true;
    document.getElementById("game-info").innerHTML = "Game in progress.";

    // Reset grid colors
    for (var r = 0; r < 6; ++r) {
        for (var c = 0; c < 9; ++c) {
            var cell = document.getElementById(`cell-${r * 9 + c}`);
            if (r === 0 && c === 0) {
                cell.bgColor = 'red';
            } else {
                cell.bgColor = 'white';
            }
        }
    }

    // Send field reset ajax-post
    var token = document.getElementById('RequestVerificationToken').value;
    await fetch('/?handler=Action', {
        method: 'POST',
        headers: {
            "RequestVerificationToken": token,
            "Content-Type": "application/json"
        },
        body: JSON.stringify({
            x: -1,
            y: -1,
            reset: true
        })
    })
        .then(response => response.json());
}