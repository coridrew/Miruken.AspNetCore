"use strict";

var connection = new signalR.HubConnectionBuilder()
    .withUrl("/hub/miruken")
    .build();

//Disable send button until connection is established
document.getElementById("createPlayer").disabled = true;

connection.on("Publish", ({ payload }) => {
    const player = payload.player,
          li     = document.createElement("li");
    li.textContent = `Player ${player.id} created (${player.name})`;
    document.getElementById("messagesList").appendChild(li);
});

connection.start().then(function () {
    document.getElementById("createPlayer").disabled = false;
}).catch(function (err) {
    return console.error(err.toString());
});

document.getElementById("createPlayer").addEventListener("click", function (event) {
    const name = document.getElementById("playerName").value,
          dob  = document.getElementById("playerDOB").value;

    connection.invoke("Process", {
            "payload": {
                "$type": "Miruken.AspNetCore.Tests.CreatePlayer, Miruken.AspNetCore.Tests",
                "player": {
                    "name": name,
                    "person": {
                        "dob": dob
                    }
                }
            }
        })
        .then(({ payload }) => {
            const player = payload.player,
                  li     = document.createElement("li");
            li.textContent = `Player ${player.id} created (${player.name})`;
            document.getElementById("messagesList").appendChild(li);
            connection.invoke("Publish", { payload });
        })
        .catch(err => {
            return console.error(err.toString());
        });

    event.preventDefault();
});