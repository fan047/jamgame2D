(function(root){const defaults={
  "settings": {
    "schemaVersion": 1,
    "version": "mid-1",
    "ballsPerRound": 12,
    "ballRadius": 0.12,
    "ballSpeed": 12,
    "launchInterval": 0.06,
    "maxAimAngle": 80,
    "recallTimeout": 20
  },
  "level": {
    "schemaVersion": 2,
    "version": "mid-10",
    "levels": [
      {
        "id": "stage-1",
        "name": "第 1 关",
        "initialBlocks": [
          {
            "column": 0,
            "row": 0,
            "width": 2,
            "health": 5,
            "target": false,
            "height": 1,
            "rewardBalls": 0
          },
          {
            "column": 3,
            "row": 0,
            "width": 2,
            "height": 1,
            "health": 14,
            "target": false,
            "rewardBalls": 0
          },
          {
            "column": 6,
            "row": 0,
            "width": 2,
            "height": 1,
            "health": 15,
            "target": false,
            "rewardBalls": 0
          },
          {
            "column": 0,
            "row": 1,
            "width": 1,
            "health": 3,
            "target": false,
            "height": 1,
            "rewardBalls": 0
          },
          {
            "column": 2,
            "row": 1,
            "width": 1,
            "health": 3,
            "target": false,
            "height": 1,
            "rewardBalls": 0
          },
          {
            "column": 5,
            "row": 1,
            "width": 1,
            "health": 3,
            "target": false,
            "height": 1,
            "rewardBalls": 0
          },
          {
            "column": 7,
            "row": 1,
            "width": 1,
            "health": 3,
            "target": false,
            "height": 1,
            "rewardBalls": 0
          },
          {
            "column": 1,
            "row": 2,
            "width": 2,
            "height": 1,
            "health": 28,
            "target": false,
            "rewardBalls": 3
          },
          {
            "column": 5,
            "row": 2,
            "width": 2,
            "health": 4,
            "target": false,
            "height": 1,
            "rewardBalls": 0
          },
          {
            "column": 0,
            "row": 3,
            "width": 2,
            "height": 1,
            "health": 13,
            "target": false,
            "rewardBalls": 0
          },
          {
            "column": 6,
            "row": 3,
            "width": 2,
            "health": 3,
            "target": false,
            "height": 1,
            "rewardBalls": 0
          },
          {
            "column": 1,
            "row": 6,
            "width": 2,
            "height": 1,
            "health": 5,
            "target": false,
            "rewardBalls": 0
          },
          {
            "column": 3,
            "row": 3,
            "width": 1,
            "height": 2,
            "health": 13,
            "target": false,
            "rewardBalls": 0
          }
        ],
        "incomingRows": [
          {
            "blocks": [
              {
                "column": 1,
                "row": 0,
                "width": 1,
                "height": 1,
                "health": 6,
                "target": false,
                "rewardBalls": 0
              },
              {
                "column": 3,
                "row": 0,
                "width": 2,
                "height": 1,
                "health": 10,
                "target": false,
                "rewardBalls": 0
              },
              {
                "column": 6,
                "row": 0,
                "width": 1,
                "health": 3,
                "target": false,
                "height": 1,
                "rewardBalls": 0
              }
            ]
          },
          {
            "blocks": [
              {
                "column": 0,
                "row": 0,
                "width": 2,
                "height": 1,
                "health": 23,
                "target": false,
                "rewardBalls": 0
              },
              {
                "column": 6,
                "row": 0,
                "width": 2,
                "height": 1,
                "health": 13,
                "target": false,
                "rewardBalls": 0
              }
            ]
          },
          {
            "blocks": [
              {
                "column": 1,
                "row": 0,
                "width": 2,
                "health": 4,
                "target": false,
                "height": 1,
                "rewardBalls": 0
              },
              {
                "column": 5,
                "row": 0,
                "width": 2,
                "health": 4,
                "target": false,
                "height": 1,
                "rewardBalls": 0
              }
            ]
          },
          {
            "blocks": [
              {
                "column": 0,
                "row": 0,
                "width": 2,
                "height": 1,
                "health": 27,
                "target": true,
                "rewardBalls": 0
              }
            ]
          }
        ]
      },
      {
        "id": "stage-2",
        "name": "第 2 关",
        "initialBlocks": [
          {
            "column": 0,
            "row": 0,
            "width": 2,
            "health": 6,
            "target": false,
            "height": 1,
            "rewardBalls": 0
          },
          {
            "column": 3,
            "row": 0,
            "width": 2,
            "health": 5,
            "target": false,
            "height": 1,
            "rewardBalls": 0
          },
          {
            "column": 6,
            "row": 0,
            "width": 2,
            "health": 6,
            "target": false,
            "height": 1,
            "rewardBalls": 0
          },
          {
            "column": 0,
            "row": 1,
            "width": 1,
            "health": 4,
            "target": false,
            "height": 1,
            "rewardBalls": 0
          },
          {
            "column": 2,
            "row": 1,
            "width": 1,
            "health": 4,
            "target": false,
            "height": 1,
            "rewardBalls": 0
          },
          {
            "column": 5,
            "row": 1,
            "width": 1,
            "health": 4,
            "target": false,
            "height": 1,
            "rewardBalls": 0
          },
          {
            "column": 7,
            "row": 1,
            "width": 1,
            "health": 4,
            "target": false,
            "height": 1,
            "rewardBalls": 0
          },
          {
            "column": 3,
            "row": 5,
            "width": 2,
            "height": 1,
            "health": 28,
            "target": false,
            "rewardBalls": 0
          },
          {
            "column": 2,
            "row": 2,
            "width": 1,
            "height": 2,
            "health": 10,
            "target": false,
            "rewardBalls": 0
          },
          {
            "column": 5,
            "row": 2,
            "width": 1,
            "height": 2,
            "health": 20,
            "target": false,
            "rewardBalls": 0
          },
          {
            "column": 3,
            "row": 2,
            "width": 2,
            "height": 1,
            "health": 20,
            "target": false,
            "rewardBalls": 3
          },
          {
            "column": 6,
            "row": 3,
            "width": 1,
            "height": 1,
            "health": 3,
            "target": false,
            "rewardBalls": 0
          },
          {
            "column": 1,
            "row": 3,
            "width": 1,
            "height": 1,
            "health": 13,
            "target": false,
            "rewardBalls": 0
          },
          {
            "column": 0,
            "row": 6,
            "width": 1,
            "height": 1,
            "health": 3,
            "target": false,
            "rewardBalls": 0
          },
          {
            "column": 7,
            "row": 6,
            "width": 1,
            "height": 1,
            "health": 3,
            "target": false,
            "rewardBalls": 0
          }
        ],
        "incomingRows": [
          {
            "blocks": [
              {
                "column": 1,
                "row": 0,
                "width": 1,
                "height": 1,
                "health": 14,
                "target": false,
                "rewardBalls": 0
              },
              {
                "column": 3,
                "row": 0,
                "width": 2,
                "health": 5,
                "target": false,
                "height": 1,
                "rewardBalls": 0
              },
              {
                "column": 6,
                "row": 0,
                "width": 1,
                "height": 1,
                "health": 14,
                "target": false,
                "rewardBalls": 0
              }
            ]
          },
          {
            "blocks": [
              {
                "column": 0,
                "row": 0,
                "width": 2,
                "height": 1,
                "health": 10,
                "target": false,
                "rewardBalls": 0
              },
              {
                "column": 6,
                "row": 0,
                "width": 2,
                "height": 1,
                "health": 10,
                "target": false,
                "rewardBalls": 0
              },
              {
                "column": 4,
                "row": 0,
                "width": 1,
                "height": 1,
                "health": 30,
                "target": false,
                "rewardBalls": 0
              }
            ]
          },
          {
            "blocks": [
              {
                "column": 1,
                "row": 0,
                "width": 2,
                "height": 1,
                "health": 25,
                "target": false,
                "rewardBalls": 0
              },
              {
                "column": 5,
                "row": 0,
                "width": 2,
                "height": 1,
                "health": 25,
                "target": false,
                "rewardBalls": 0
              }
            ]
          },
          {
            "blocks": [
              {
                "column": 3,
                "row": 0,
                "width": 2,
                "height": 1,
                "health": 24,
                "target": true,
                "rewardBalls": 0
              }
            ]
          }
        ]
      },
      {
        "id": "stage-3",
        "name": "第 3 关",
        "initialBlocks": [
          {
            "column": 0,
            "row": 0,
            "width": 2,
            "height": 1,
            "health": 27,
            "target": false,
            "rewardBalls": 0
          },
          {
            "column": 6,
            "row": 0,
            "width": 2,
            "height": 1,
            "health": 37,
            "target": false,
            "rewardBalls": 0
          },
          {
            "column": 0,
            "row": 1,
            "width": 1,
            "height": 1,
            "health": 16,
            "target": false,
            "rewardBalls": 0
          },
          {
            "column": 2,
            "row": 1,
            "width": 1,
            "height": 1,
            "health": 15,
            "target": false,
            "rewardBalls": 0
          },
          {
            "column": 5,
            "row": 1,
            "width": 1,
            "height": 1,
            "health": 35,
            "target": false,
            "rewardBalls": 0
          },
          {
            "column": 7,
            "row": 1,
            "width": 1,
            "height": 1,
            "health": 13,
            "target": false,
            "rewardBalls": 0
          },
          {
            "column": 1,
            "row": 2,
            "width": 2,
            "height": 1,
            "health": 16,
            "target": false,
            "rewardBalls": 0
          },
          {
            "column": 5,
            "row": 2,
            "width": 2,
            "height": 1,
            "health": 16,
            "target": false,
            "rewardBalls": 0
          },
          {
            "column": 1,
            "row": 3,
            "width": 2,
            "height": 1,
            "health": 35,
            "target": false,
            "rewardBalls": 0
          },
          {
            "column": 5,
            "row": 3,
            "width": 2,
            "height": 1,
            "health": 25,
            "target": false,
            "rewardBalls": 0
          },
          {
            "column": 0,
            "row": 6,
            "width": 1,
            "height": 1,
            "health": 23,
            "target": false,
            "rewardBalls": 0
          },
          {
            "column": 4,
            "row": 1,
            "width": 1,
            "height": 1,
            "health": 23,
            "target": false,
            "rewardBalls": 0
          },
          {
            "column": 7,
            "row": 5,
            "width": 1,
            "height": 2,
            "health": 30,
            "target": false,
            "rewardBalls": 0
          }
        ],
        "incomingRows": [
          {
            "blocks": [
              {
                "column": 1,
                "row": 0,
                "width": 1,
                "height": 1,
                "health": 15,
                "target": false,
                "rewardBalls": 0
              },
              {
                "column": 3,
                "row": 0,
                "width": 2,
                "height": 1,
                "health": 16,
                "target": false,
                "rewardBalls": 0
              },
              {
                "column": 6,
                "row": 0,
                "width": 1,
                "height": 1,
                "health": 25,
                "target": false,
                "rewardBalls": 0
              }
            ]
          },
          {
            "blocks": [
              {
                "column": 0,
                "row": 0,
                "width": 2,
                "height": 1,
                "health": 36,
                "target": false,
                "rewardBalls": 0
              },
              {
                "column": 6,
                "row": 0,
                "width": 2,
                "height": 1,
                "health": 26,
                "target": false,
                "rewardBalls": 0
              },
              {
                "column": 3,
                "row": 0,
                "width": 2,
                "height": 1,
                "health": 38,
                "target": false,
                "rewardBalls": 0
              }
            ]
          },
          {
            "blocks": [
              {
                "column": 1,
                "row": 0,
                "width": 2,
                "height": 1,
                "health": 36,
                "target": false,
                "rewardBalls": 0
              },
              {
                "column": 5,
                "row": 0,
                "width": 2,
                "height": 1,
                "health": 13,
                "target": false,
                "rewardBalls": 0
              },
              {
                "column": 3,
                "row": 0,
                "width": 1,
                "height": 1,
                "health": 33,
                "target": false,
                "rewardBalls": 0
              }
            ]
          },
          {
            "blocks": [
              {
                "column": 3,
                "row": 0,
                "width": 2,
                "height": 1,
                "health": 38,
                "target": false,
                "rewardBalls": 0
              },
              {
                "column": 0,
                "row": 0,
                "width": 1,
                "height": 1,
                "health": 32,
                "target": false,
                "rewardBalls": 0
              },
              {
                "column": 6,
                "row": 0,
                "width": 2,
                "height": 1,
                "health": 32,
                "target": false,
                "rewardBalls": 0
              }
            ]
          },
          {
            "blocks": [
              {
                "column": 0,
                "row": 0,
                "width": 1,
                "height": 1,
                "health": 23,
                "target": false,
                "rewardBalls": 0
              },
              {
                "column": 2,
                "row": 0,
                "width": 2,
                "height": 1,
                "health": 31,
                "target": false,
                "rewardBalls": 0
              },
              {
                "column": 7,
                "row": 0,
                "width": 1,
                "height": 1,
                "health": 30,
                "target": false,
                "rewardBalls": 0
              }
            ]
          },
          {
            "blocks": [
              {
                "column": 2,
                "row": 0,
                "width": 2,
                "height": 1,
                "health": 31,
                "target": false,
                "rewardBalls": 0
              },
              {
                "column": 6,
                "row": 0,
                "width": 1,
                "height": 1,
                "health": 50,
                "target": false,
                "rewardBalls": 8
              },
              {
                "column": 0,
                "row": 0,
                "width": 1,
                "height": 1,
                "health": 31,
                "target": false,
                "rewardBalls": 0
              }
            ]
          },
          {
            "blocks": [
              {
                "column": 0,
                "row": 0,
                "width": 1,
                "height": 1,
                "health": 30,
                "target": true,
                "rewardBalls": 0
              }
            ]
          }
        ]
      },
      {
        "id": "stage-4",
        "name": "第 4 关",
        "initialBlocks": [
          {
            "column": 1,
            "row": 5,
            "width": 1,
            "height": 2,
            "health": 23,
            "target": false,
            "rewardBalls": 0
          },
          {
            "column": 0,
            "row": 2,
            "width": 2,
            "height": 1,
            "health": 33,
            "target": false,
            "rewardBalls": 0
          },
          {
            "column": 3,
            "row": 4,
            "width": 1,
            "height": 1,
            "health": 13,
            "target": false,
            "rewardBalls": 6
          },
          {
            "column": 6,
            "row": 1,
            "width": 2,
            "height": 1,
            "health": 43,
            "target": false,
            "rewardBalls": 0
          },
          {
            "column": 5,
            "row": 4,
            "width": 1,
            "height": 2,
            "health": 24,
            "target": false,
            "rewardBalls": 0
          },
          {
            "column": 5,
            "row": 0,
            "width": 1,
            "height": 1,
            "health": 23,
            "target": false,
            "rewardBalls": 0
          },
          {
            "column": 0,
            "row": 0,
            "width": 1,
            "height": 2,
            "health": 37,
            "target": false,
            "rewardBalls": 0
          },
          {
            "column": 4,
            "row": 3,
            "width": 2,
            "height": 1,
            "health": 45,
            "target": false,
            "rewardBalls": 0
          }
        ],
        "incomingRows": [
          {
            "blocks": [
              {
                "column": 0,
                "row": 0,
                "width": 2,
                "height": 1,
                "health": 29,
                "target": false,
                "rewardBalls": 0
              },
              {
                "column": 5,
                "row": 0,
                "width": 2,
                "height": 1,
                "health": 39,
                "target": false,
                "rewardBalls": 0
              }
            ]
          },
          {
            "blocks": [
              {
                "column": 2,
                "row": 0,
                "width": 1,
                "height": 1,
                "health": 27,
                "target": false,
                "rewardBalls": 0
              },
              {
                "column": 6,
                "row": 0,
                "width": 1,
                "height": 1,
                "health": 23,
                "target": false,
                "rewardBalls": 0
              }
            ]
          },
          {
            "blocks": [
              {
                "column": 0,
                "row": 0,
                "width": 2,
                "height": 1,
                "health": 31,
                "target": false,
                "rewardBalls": 0
              },
              {
                "column": 6,
                "row": 0,
                "width": 2,
                "height": 1,
                "health": 37,
                "target": false,
                "rewardBalls": 0
              }
            ]
          },
          {
            "blocks": [
              {
                "column": 0,
                "row": 0,
                "width": 1,
                "height": 1,
                "health": 31,
                "target": false,
                "rewardBalls": 0
              },
              {
                "column": 3,
                "row": 0,
                "width": 1,
                "height": 1,
                "health": 23,
                "target": false,
                "rewardBalls": 0
              },
              {
                "column": 5,
                "row": 0,
                "width": 1,
                "height": 1,
                "health": 19,
                "target": false,
                "rewardBalls": 0
              },
              {
                "column": 7,
                "row": 0,
                "width": 1,
                "height": 1,
                "health": 27,
                "target": false,
                "rewardBalls": 0
              }
            ]
          },
          {
            "blocks": [
              {
                "column": 0,
                "row": 0,
                "width": 2,
                "height": 1,
                "health": 55,
                "target": false,
                "rewardBalls": 6
              }
            ]
          },
          {
            "blocks": [
              {
                "column": 5,
                "row": 0,
                "width": 1,
                "height": 1,
                "health": 50,
                "target": true,
                "rewardBalls": 0
              }
            ]
          }
        ]
      },
      {
        "id": "stage-5",
        "name": "第 5 关",
        "initialBlocks": [
          {
            "column": 0,
            "row": 2,
            "width": 1,
            "height": 2,
            "health": 41,
            "target": false,
            "rewardBalls": 0
          },
          {
            "column": 0,
            "row": 4,
            "width": 1,
            "height": 2,
            "health": 60,
            "target": false,
            "rewardBalls": 0
          },
          {
            "column": 2,
            "row": 6,
            "width": 1,
            "height": 1,
            "health": 49,
            "target": false,
            "rewardBalls": 0
          },
          {
            "column": 7,
            "row": 5,
            "width": 1,
            "height": 1,
            "health": 56,
            "target": false,
            "rewardBalls": 0
          },
          {
            "column": 7,
            "row": 2,
            "width": 1,
            "height": 2,
            "health": 59,
            "target": false,
            "rewardBalls": 0
          },
          {
            "column": 2,
            "row": 2,
            "width": 2,
            "height": 1,
            "health": 65,
            "target": false,
            "rewardBalls": 0
          },
          {
            "column": 6,
            "row": 0,
            "width": 1,
            "height": 2,
            "health": 54,
            "target": false,
            "rewardBalls": 0
          },
          {
            "column": 5,
            "row": 2,
            "width": 1,
            "height": 1,
            "health": 59,
            "target": false,
            "rewardBalls": 0
          },
          {
            "column": 1,
            "row": 0,
            "width": 2,
            "height": 1,
            "health": 50,
            "target": false,
            "rewardBalls": 0
          },
          {
            "column": 4,
            "row": 0,
            "width": 1,
            "height": 1,
            "health": 51,
            "target": false,
            "rewardBalls": 0
          },
          {
            "column": 4,
            "row": 5,
            "width": 2,
            "height": 1,
            "health": 52,
            "target": false,
            "rewardBalls": 0
          }
        ],
        "incomingRows": [
          {
            "blocks": [
              {
                "column": 3,
                "row": 0,
                "width": 1,
                "height": 1,
                "health": 50,
                "target": false,
                "rewardBalls": 0
              },
              {
                "column": 0,
                "row": 0,
                "width": 2,
                "height": 1,
                "health": 50,
                "target": false,
                "rewardBalls": 0
              },
              {
                "column": 5,
                "row": 0,
                "width": 2,
                "height": 1,
                "health": 49,
                "target": false,
                "rewardBalls": 0
              },
              {
                "column": 7,
                "row": 0,
                "width": 1,
                "height": 1,
                "health": 50,
                "target": false,
                "rewardBalls": 0
              }
            ]
          },
          {
            "blocks": [
              {
                "column": 0,
                "row": 0,
                "width": 1,
                "height": 1,
                "health": 43,
                "target": false,
                "rewardBalls": 0
              },
              {
                "column": 5,
                "row": 0,
                "width": 1,
                "height": 1,
                "health": 43,
                "target": false,
                "rewardBalls": 0
              },
              {
                "column": 2,
                "row": 0,
                "width": 1,
                "height": 1,
                "health": 56,
                "target": false,
                "rewardBalls": 0
              }
            ]
          },
          {
            "blocks": [
              {
                "column": 0,
                "row": 0,
                "width": 1,
                "height": 1,
                "health": 52,
                "target": false,
                "rewardBalls": 0
              },
              {
                "column": 7,
                "row": 0,
                "width": 1,
                "height": 1,
                "health": 39,
                "target": false,
                "rewardBalls": 0
              },
              {
                "column": 4,
                "row": 0,
                "width": 1,
                "height": 1,
                "health": 50,
                "target": false,
                "rewardBalls": 0
              }
            ]
          },
          {
            "blocks": [
              {
                "column": 0,
                "row": 0,
                "width": 2,
                "height": 1,
                "health": 53,
                "target": false,
                "rewardBalls": 0
              },
              {
                "column": 2,
                "row": 0,
                "width": 2,
                "height": 1,
                "health": 54,
                "target": false,
                "rewardBalls": 0
              }
            ]
          },
          {
            "blocks": [
              {
                "column": 6,
                "row": 0,
                "width": 2,
                "height": 1,
                "health": 55,
                "target": false,
                "rewardBalls": 0
              },
              {
                "column": 0,
                "row": 0,
                "width": 1,
                "height": 1,
                "health": 70,
                "target": false,
                "rewardBalls": 6
              }
            ]
          },
          {
            "blocks": [
              {
                "column": 4,
                "row": 0,
                "width": 2,
                "height": 1,
                "health": 53,
                "target": false,
                "rewardBalls": 0
              }
            ]
          },
          {
            "blocks": [
              {
                "column": 1,
                "row": 0,
                "width": 2,
                "height": 1,
                "health": 52,
                "target": false,
                "rewardBalls": 0
              }
            ]
          },
          {
            "blocks": [
              {
                "column": 4,
                "row": 0,
                "width": 2,
                "height": 1,
                "health": 170,
                "target": false,
                "rewardBalls": 8
              },
              {
                "column": 0,
                "row": 0,
                "width": 2,
                "height": 1,
                "health": 65,
                "target": false,
                "rewardBalls": 0
              }
            ]
          },
          {
            "blocks": [
              {
                "column": 6,
                "row": 0,
                "width": 2,
                "height": 1,
                "health": 82,
                "target": true,
                "rewardBalls": 0
              }
            ]
          }
        ]
      },
      {
        "id": "stage-6",
        "name": "第 6 关",
        "initialBlocks": [
          {
            "column": 0,
            "row": 1,
            "width": 1,
            "height": 2,
            "health": 50,
            "target": false,
            "rewardBalls": 0
          },
          {
            "column": 0,
            "row": 3,
            "width": 1,
            "height": 2,
            "health": 60,
            "target": false,
            "rewardBalls": 0
          },
          {
            "column": 1,
            "row": 5,
            "width": 2,
            "height": 1,
            "health": 53,
            "target": false,
            "rewardBalls": 0
          },
          {
            "column": 3,
            "row": 5,
            "width": 2,
            "height": 1,
            "health": 52,
            "target": false,
            "rewardBalls": 0
          },
          {
            "column": 5,
            "row": 3,
            "width": 1,
            "height": 1,
            "health": 33,
            "target": false,
            "rewardBalls": 0
          },
          {
            "column": 5,
            "row": 1,
            "width": 1,
            "height": 2,
            "health": 65,
            "target": false,
            "rewardBalls": 0
          },
          {
            "column": 2,
            "row": 2,
            "width": 1,
            "height": 1,
            "health": 40,
            "target": false,
            "rewardBalls": 0
          },
          {
            "column": 1,
            "row": 0,
            "width": 1,
            "height": 1,
            "health": 50,
            "target": false,
            "rewardBalls": 0
          },
          {
            "column": 6,
            "row": 6,
            "width": 2,
            "height": 1,
            "health": 30,
            "target": false,
            "rewardBalls": 2
          }
        ],
        "incomingRows": [
          {
            "blocks": [
              {
                "column": 1,
                "row": 0,
                "width": 1,
                "height": 1,
                "health": 45,
                "target": false,
                "rewardBalls": 0
              },
              {
                "column": 5,
                "row": 0,
                "width": 1,
                "height": 1,
                "health": 62,
                "target": false,
                "rewardBalls": 0
              }
            ]
          },
          {
            "blocks": [
              {
                "column": 1,
                "row": 0,
                "width": 1,
                "height": 1,
                "health": 51,
                "target": false,
                "rewardBalls": 0
              },
              {
                "column": 5,
                "row": 0,
                "width": 1,
                "height": 1,
                "health": 60,
                "target": false,
                "rewardBalls": 5
              },
              {
                "column": 0,
                "row": 0,
                "width": 1,
                "height": 1,
                "health": 32,
                "target": false,
                "rewardBalls": 0
              }
            ]
          },
          {
            "blocks": [
              {
                "column": 3,
                "row": 0,
                "width": 2,
                "height": 1,
                "health": 68,
                "target": false,
                "rewardBalls": 0
              }
            ]
          },
          {
            "blocks": [
              {
                "column": 0,
                "row": 0,
                "width": 1,
                "height": 1,
                "health": 63,
                "target": false,
                "rewardBalls": 0
              },
              {
                "column": 1,
                "row": 0,
                "width": 1,
                "height": 1,
                "health": 73,
                "target": false,
                "rewardBalls": 0
              },
              {
                "column": 2,
                "row": 0,
                "width": 2,
                "height": 1,
                "health": 53,
                "target": false,
                "rewardBalls": 0
              }
            ]
          },
          {
            "blocks": [
              {
                "column": 6,
                "row": 0,
                "width": 1,
                "height": 1,
                "health": 70,
                "target": false,
                "rewardBalls": 0
              }
            ]
          },
          {
            "blocks": [
              {
                "column": 1,
                "row": 0,
                "width": 1,
                "height": 1,
                "health": 67,
                "target": false,
                "rewardBalls": 0
              },
              {
                "column": 3,
                "row": 0,
                "width": 2,
                "height": 1,
                "health": 66,
                "target": false,
                "rewardBalls": 0
              },
              {
                "column": 6,
                "row": 0,
                "width": 2,
                "height": 1,
                "health": 60,
                "target": false,
                "rewardBalls": 0
              }
            ]
          },
          {
            "blocks": [
              {
                "column": 0,
                "row": 0,
                "width": 2,
                "height": 1,
                "health": 53,
                "target": false,
                "rewardBalls": 0
              },
              {
                "column": 3,
                "row": 0,
                "width": 1,
                "height": 1,
                "health": 61,
                "target": false,
                "rewardBalls": 0
              },
              {
                "column": 2,
                "row": 0,
                "width": 1,
                "height": 1,
                "health": 60,
                "target": false,
                "rewardBalls": 0
              },
              {
                "column": 7,
                "row": 0,
                "width": 1,
                "height": 1,
                "health": 200,
                "target": false,
                "rewardBalls": 8
              }
            ]
          },
          {
            "blocks": [
              {
                "column": 0,
                "row": 0,
                "width": 1,
                "height": 1,
                "health": 61,
                "target": false,
                "rewardBalls": 0
              },
              {
                "column": 2,
                "row": 0,
                "width": 1,
                "height": 1,
                "health": 70,
                "target": false,
                "rewardBalls": 0
              },
              {
                "column": 6,
                "row": 0,
                "width": 1,
                "height": 1,
                "health": 70,
                "target": true,
                "rewardBalls": 0
              }
            ]
          }
        ]
      },
      {
        "id": "stage-7",
        "name": "第 7 关",
        "initialBlocks": [
          {
            "column": 0,
            "row": 0,
            "width": 2,
            "height": 1,
            "health": 125,
            "target": false,
            "rewardBalls": 0
          },
          {
            "column": 2,
            "row": 0,
            "width": 2,
            "height": 1,
            "health": 104,
            "target": false,
            "rewardBalls": 0
          },
          {
            "column": 4,
            "row": 0,
            "width": 2,
            "height": 1,
            "health": 93,
            "target": false,
            "rewardBalls": 0
          },
          {
            "column": 7,
            "row": 3,
            "width": 1,
            "height": 2,
            "health": 103,
            "target": false,
            "rewardBalls": 0
          },
          {
            "column": 0,
            "row": 1,
            "width": 1,
            "height": 1,
            "health": 112,
            "target": false,
            "rewardBalls": 0
          },
          {
            "column": 0,
            "row": 3,
            "width": 1,
            "height": 2,
            "health": 110,
            "target": false,
            "rewardBalls": 0
          },
          {
            "column": 7,
            "row": 1,
            "width": 1,
            "height": 1,
            "health": 123,
            "target": false,
            "rewardBalls": 0
          },
          {
            "column": 3,
            "row": 4,
            "width": 1,
            "height": 2,
            "health": 105,
            "target": false,
            "rewardBalls": 0
          },
          {
            "column": 4,
            "row": 3,
            "width": 2,
            "height": 1,
            "health": 123,
            "target": false,
            "rewardBalls": 0
          },
          {
            "column": 0,
            "row": 5,
            "width": 1,
            "height": 1,
            "health": 98,
            "target": false,
            "rewardBalls": 0
          },
          {
            "column": 7,
            "row": 5,
            "width": 1,
            "height": 1,
            "health": 89,
            "target": false,
            "rewardBalls": 0
          },
          {
            "column": 1,
            "row": 6,
            "width": 2,
            "height": 1,
            "health": 60,
            "target": false,
            "rewardBalls": 0
          }
        ],
        "incomingRows": [
          {
            "blocks": [
              {
                "column": 4,
                "row": 0,
                "width": 1,
                "height": 1,
                "health": 145,
                "target": false,
                "rewardBalls": 0
              },
              {
                "column": 1,
                "row": 0,
                "width": 2,
                "height": 1,
                "health": 133,
                "target": false,
                "rewardBalls": 0
              }
            ]
          },
          {
            "blocks": [
              {
                "column": 3,
                "row": 0,
                "width": 1,
                "height": 1,
                "health": 167,
                "target": false,
                "rewardBalls": 0
              },
              {
                "column": 6,
                "row": 0,
                "width": 2,
                "height": 1,
                "health": 130,
                "target": false,
                "rewardBalls": 0
              }
            ]
          },
          {
            "blocks": [
              {
                "column": 0,
                "row": 0,
                "width": 1,
                "height": 1,
                "health": 130,
                "target": false,
                "rewardBalls": 0
              },
              {
                "column": 5,
                "row": 0,
                "width": 1,
                "height": 1,
                "health": 130,
                "target": false,
                "rewardBalls": 0
              }
            ]
          },
          {
            "blocks": [
              {
                "column": 1,
                "row": 0,
                "width": 1,
                "height": 1,
                "health": 138,
                "target": false,
                "rewardBalls": 0
              },
              {
                "column": 6,
                "row": 0,
                "width": 1,
                "height": 1,
                "health": 136,
                "target": false,
                "rewardBalls": 0
              }
            ]
          },
          {
            "blocks": [
              {
                "column": 0,
                "row": 0,
                "width": 1,
                "height": 1,
                "health": 130,
                "target": false,
                "rewardBalls": 0
              },
              {
                "column": 3,
                "row": 0,
                "width": 2,
                "height": 1,
                "health": 123,
                "target": false,
                "rewardBalls": 0
              },
              {
                "column": 2,
                "row": 0,
                "width": 1,
                "height": 1,
                "health": 111,
                "target": false,
                "rewardBalls": 0
              }
            ]
          },
          {
            "blocks": [
              {
                "column": 5,
                "row": 0,
                "width": 1,
                "height": 1,
                "health": 169,
                "target": false,
                "rewardBalls": 9
              }
            ]
          },
          {
            "blocks": [
              {
                "column": 5,
                "row": 0,
                "width": 1,
                "height": 1,
                "health": 155,
                "target": false,
                "rewardBalls": 0
              },
              {
                "column": 1,
                "row": 0,
                "width": 2,
                "height": 1,
                "health": 145,
                "target": false,
                "rewardBalls": 0
              }
            ]
          },
          {
            "blocks": [
              {
                "column": 6,
                "row": 0,
                "width": 2,
                "height": 1,
                "health": 124,
                "target": false,
                "rewardBalls": 0
              },
              {
                "column": 1,
                "row": 0,
                "width": 2,
                "height": 1,
                "health": 151,
                "target": false,
                "rewardBalls": 0
              }
            ]
          },
          {
            "blocks": [
              {
                "column": 0,
                "row": 0,
                "width": 1,
                "height": 1,
                "health": 134,
                "target": false,
                "rewardBalls": 0
              },
              {
                "column": 3,
                "row": 0,
                "width": 1,
                "height": 1,
                "health": 140,
                "target": false,
                "rewardBalls": 7
              }
            ]
          },
          {
            "blocks": [
              {
                "column": 7,
                "row": 0,
                "width": 1,
                "height": 1,
                "health": 210,
                "target": true,
                "rewardBalls": 0
              },
              {
                "column": 5,
                "row": 0,
                "width": 1,
                "height": 1,
                "health": 155,
                "target": false,
                "rewardBalls": 8
              },
              {
                "column": 1,
                "row": 0,
                "width": 2,
                "height": 1,
                "health": 300,
                "target": false,
                "rewardBalls": 0
              }
            ]
          }
        ]
      },
      {
        "id": "stage-8",
        "name": "第 8 关",
        "initialBlocks": [
          {
            "column": 0,
            "row": 5,
            "width": 2,
            "height": 1,
            "health": 133,
            "target": false,
            "rewardBalls": 0
          },
          {
            "column": 3,
            "row": 5,
            "width": 2,
            "height": 1,
            "health": 155,
            "target": false,
            "rewardBalls": 0
          },
          {
            "column": 0,
            "row": 2,
            "width": 1,
            "height": 2,
            "health": 177,
            "target": false,
            "rewardBalls": 0
          },
          {
            "column": 1,
            "row": 1,
            "width": 1,
            "height": 1,
            "health": 139,
            "target": false,
            "rewardBalls": 7
          },
          {
            "column": 0,
            "row": 0,
            "width": 1,
            "height": 1,
            "health": 98,
            "target": false,
            "rewardBalls": 0
          },
          {
            "column": 2,
            "row": 0,
            "width": 2,
            "height": 1,
            "health": 156,
            "target": false,
            "rewardBalls": 0
          },
          {
            "column": 6,
            "row": 5,
            "width": 1,
            "height": 1,
            "health": 121,
            "target": false,
            "rewardBalls": 0
          },
          {
            "column": 5,
            "row": 0,
            "width": 2,
            "height": 1,
            "health": 122,
            "target": false,
            "rewardBalls": 0
          },
          {
            "column": 4,
            "row": 4,
            "width": 1,
            "height": 1,
            "health": 136,
            "target": false,
            "rewardBalls": 0
          },
          {
            "column": 6,
            "row": 2,
            "width": 1,
            "height": 1,
            "health": 123,
            "target": false,
            "rewardBalls": 0
          },
          {
            "column": 7,
            "row": 4,
            "width": 1,
            "height": 1,
            "health": 147,
            "target": false,
            "rewardBalls": 0
          }
        ],
        "incomingRows": [
          {
            "blocks": [
              {
                "column": 1,
                "row": 0,
                "width": 1,
                "height": 1,
                "health": 122,
                "target": false,
                "rewardBalls": 0
              },
              {
                "column": 4,
                "row": 0,
                "width": 1,
                "height": 1,
                "health": 108,
                "target": false,
                "rewardBalls": 0
              }
            ]
          },
          {
            "blocks": [
              {
                "column": 3,
                "row": 0,
                "width": 1,
                "height": 1,
                "health": 166,
                "target": false,
                "rewardBalls": 0
              }
            ]
          },
          {
            "blocks": [
              {
                "column": 2,
                "row": 0,
                "width": 1,
                "height": 1,
                "health": 129,
                "target": false,
                "rewardBalls": 0
              },
              {
                "column": 5,
                "row": 0,
                "width": 1,
                "height": 1,
                "health": 199,
                "target": false,
                "rewardBalls": 10
              },
              {
                "column": 0,
                "row": 0,
                "width": 1,
                "height": 1,
                "health": 123,
                "target": false,
                "rewardBalls": 0
              }
            ]
          },
          {
            "blocks": [
              {
                "column": 1,
                "row": 0,
                "width": 1,
                "height": 1,
                "health": 155,
                "target": false,
                "rewardBalls": 0
              },
              {
                "column": 6,
                "row": 0,
                "width": 1,
                "height": 1,
                "health": 159,
                "target": false,
                "rewardBalls": 0
              },
              {
                "column": 3,
                "row": 0,
                "width": 1,
                "height": 1,
                "health": 147,
                "target": false,
                "rewardBalls": 0
              }
            ]
          },
          {
            "blocks": [
              {
                "column": 4,
                "row": 0,
                "width": 1,
                "height": 1,
                "health": 167,
                "target": false,
                "rewardBalls": 0
              },
              {
                "column": 6,
                "row": 0,
                "width": 1,
                "height": 1,
                "health": 149,
                "target": false,
                "rewardBalls": 0
              }
            ]
          },
          {
            "blocks": [
              {
                "column": 3,
                "row": 0,
                "width": 1,
                "height": 1,
                "health": 144,
                "target": false,
                "rewardBalls": 0
              },
              {
                "column": 5,
                "row": 0,
                "width": 1,
                "height": 1,
                "health": 166,
                "target": false,
                "rewardBalls": 0
              },
              {
                "column": 7,
                "row": 0,
                "width": 1,
                "height": 1,
                "health": 176,
                "target": false,
                "rewardBalls": 0
              },
              {
                "column": 1,
                "row": 0,
                "width": 1,
                "height": 1,
                "health": 177,
                "target": false,
                "rewardBalls": 7
              }
            ]
          },
          {
            "blocks": [
              {
                "column": 4,
                "row": 0,
                "width": 1,
                "height": 1,
                "health": 125,
                "target": false,
                "rewardBalls": 8
              },
              {
                "column": 0,
                "row": 0,
                "width": 1,
                "height": 1,
                "health": 143,
                "target": false,
                "rewardBalls": 0
              },
              {
                "column": 2,
                "row": 0,
                "width": 1,
                "height": 1,
                "health": 134,
                "target": false,
                "rewardBalls": 0
              },
              {
                "column": 7,
                "row": 0,
                "width": 1,
                "height": 1,
                "health": 188,
                "target": false,
                "rewardBalls": 0
              }
            ]
          },
          {
            "blocks": [
              {
                "column": 2,
                "row": 0,
                "width": 1,
                "height": 1,
                "health": 251,
                "target": true,
                "rewardBalls": 0
              }
            ]
          }
        ]
      },
      {
        "id": "stage-9",
        "name": "第 9 关",
        "initialBlocks": [
          {
            "column": 0,
            "row": 5,
            "width": 2,
            "height": 1,
            "health": 166,
            "target": false,
            "rewardBalls": 0
          },
          {
            "column": 0,
            "row": 3,
            "width": 2,
            "height": 1,
            "health": 155,
            "target": false,
            "rewardBalls": 0
          },
          {
            "column": 3,
            "row": 3,
            "width": 2,
            "height": 1,
            "health": 144,
            "target": false,
            "rewardBalls": 0
          },
          {
            "column": 6,
            "row": 5,
            "width": 2,
            "height": 1,
            "health": 188,
            "target": false,
            "rewardBalls": 0
          },
          {
            "column": 6,
            "row": 3,
            "width": 2,
            "height": 1,
            "health": 133,
            "target": false,
            "rewardBalls": 0
          },
          {
            "column": 2,
            "row": 0,
            "width": 1,
            "height": 2,
            "health": 122,
            "target": false,
            "rewardBalls": 0
          },
          {
            "column": 5,
            "row": 0,
            "width": 1,
            "height": 2,
            "health": 133,
            "target": false,
            "rewardBalls": 0
          },
          {
            "column": 0,
            "row": 0,
            "width": 1,
            "height": 1,
            "health": 165,
            "target": false,
            "rewardBalls": 0
          },
          {
            "column": 7,
            "row": 0,
            "width": 1,
            "height": 1,
            "health": 187,
            "target": false,
            "rewardBalls": 0
          }
        ],
        "incomingRows": [
          {
            "blocks": [
              {
                "column": 1,
                "row": 0,
                "width": 2,
                "height": 1,
                "health": 198,
                "target": false,
                "rewardBalls": 0
              },
              {
                "column": 5,
                "row": 0,
                "width": 2,
                "height": 1,
                "health": 197,
                "target": false,
                "rewardBalls": 0
              }
            ]
          },
          {
            "blocks": [
              {
                "column": 1,
                "row": 0,
                "width": 2,
                "height": 1,
                "health": 207,
                "target": false,
                "rewardBalls": 0
              },
              {
                "column": 5,
                "row": 0,
                "width": 2,
                "height": 1,
                "health": 209,
                "target": false,
                "rewardBalls": 0
              }
            ]
          },
          {
            "blocks": [
              {
                "column": 3,
                "row": 0,
                "width": 2,
                "height": 1,
                "health": 199,
                "target": false,
                "rewardBalls": 0
              },
              {
                "column": 7,
                "row": 0,
                "width": 1,
                "height": 1,
                "health": 190,
                "target": false,
                "rewardBalls": 0
              },
              {
                "column": 1,
                "row": 0,
                "width": 1,
                "height": 1,
                "health": 188,
                "target": false,
                "rewardBalls": 0
              }
            ]
          },
          {
            "blocks": [
              {
                "column": 5,
                "row": 0,
                "width": 1,
                "height": 1,
                "health": 179,
                "target": false,
                "rewardBalls": 0
              },
              {
                "column": 0,
                "row": 0,
                "width": 1,
                "height": 1,
                "health": 177,
                "target": false,
                "rewardBalls": 0
              },
              {
                "column": 2,
                "row": 0,
                "width": 1,
                "height": 1,
                "health": 188,
                "target": false,
                "rewardBalls": 0
              }
            ]
          },
          {
            "blocks": [
              {
                "column": 0,
                "row": 0,
                "width": 1,
                "height": 1,
                "health": 179,
                "target": false,
                "rewardBalls": 0
              },
              {
                "column": 4,
                "row": 0,
                "width": 1,
                "height": 1,
                "health": 190,
                "target": false,
                "rewardBalls": 0
              },
              {
                "column": 6,
                "row": 0,
                "width": 2,
                "height": 1,
                "health": 169,
                "target": false,
                "rewardBalls": 0
              }
            ]
          },
          {
            "blocks": [
              {
                "column": 1,
                "row": 0,
                "width": 1,
                "height": 1,
                "health": 200,
                "target": false,
                "rewardBalls": 0
              },
              {
                "column": 5,
                "row": 0,
                "width": 1,
                "height": 1,
                "health": 201,
                "target": false,
                "rewardBalls": 0
              }
            ]
          },
          {
            "blocks": [
              {
                "column": 1,
                "row": 0,
                "width": 2,
                "height": 1,
                "health": 203,
                "target": false,
                "rewardBalls": 0
              },
              {
                "column": 4,
                "row": 0,
                "width": 2,
                "height": 1,
                "health": 166,
                "target": false,
                "rewardBalls": 0
              },
              {
                "column": 7,
                "row": 0,
                "width": 1,
                "height": 1,
                "health": 178,
                "target": false,
                "rewardBalls": 0
              }
            ]
          },
          {
            "blocks": [
              {
                "column": 1,
                "row": 0,
                "width": 1,
                "height": 1,
                "health": 194,
                "target": false,
                "rewardBalls": 0
              },
              {
                "column": 4,
                "row": 0,
                "width": 1,
                "height": 1,
                "health": 155,
                "target": false,
                "rewardBalls": 0
              }
            ]
          },
          {
            "blocks": [
              {
                "column": 0,
                "row": 0,
                "width": 1,
                "height": 1,
                "health": 167,
                "target": false,
                "rewardBalls": 0
              },
              {
                "column": 3,
                "row": 0,
                "width": 2,
                "height": 1,
                "health": 158,
                "target": false,
                "rewardBalls": 0
              }
            ]
          },
          {
            "blocks": [
              {
                "column": 1,
                "row": 0,
                "width": 2,
                "height": 1,
                "health": 233,
                "target": false,
                "rewardBalls": 0
              },
              {
                "column": 5,
                "row": 0,
                "width": 2,
                "height": 1,
                "health": 233,
                "target": false,
                "rewardBalls": 0
              },
              {
                "column": 3,
                "row": 0,
                "width": 2,
                "height": 1,
                "health": 300,
                "target": true,
                "rewardBalls": 0
              }
            ]
          }
        ]
      }
    ]
  }
};if(typeof module==='object'&&module.exports)module.exports=defaults;else root.PlannerDefaults=defaults;})(typeof globalThis!=='undefined'?globalThis:this);
