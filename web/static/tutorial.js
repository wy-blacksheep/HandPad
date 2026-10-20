document.addEventListener('DOMContentLoaded', function () {
    const popup = document.getElementById('paste-popup');
    const pasteButton = document.getElementById('paste-button');

    // |------------------------------------------------------
    // | Control Functions
    // |------------------------------------------------------

    async function showContextMenu(x, y) {
        clipboard = await getClipboardContent();
        pasteButton.disabled = clipboard === "";
        pasteButton.innerHTML = clipboard === "" ? "Clipboard Empty" : clipboard.startsWith("IMGURL_") ? "Paste Image" : "Paste Text";

        popup.style.left = `${x}px`;
        popup.style.top = `${y}px`;
        popup.style.display = 'block';

        isContextMenuVisible = true;
    }

    function hideContextMenu() {
        popup.style.display = 'none';
        isContextMenuVisible = false;
    }

    function copySelectionToClipboard() {
        fetch('/api/clipboard', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ clipboard: selectedContent })
        })
        .then(response => response.json())
        .then(data => {
            if(data.status === "success") {
                console.log("Item Copied: ", selectedContent);
                getClipboardContent();
            } else {
                console.error('Failed to copy content:', data.message);
            }
        })
        .catch(err => {
            console.error('Failed to copy content:', err);
        });
    }


    async function getClipboardContent() {
        try {
            let response = await fetch('/api/clipboard', { method: 'GET' });
            let data = await response.json();
            console.log('Clipboard Content:', data.clipboard);
            return data.clipboard;
        } catch(err) {
            console.error('Failed to read clipboard content:', err);
        }
    }

    // event listener for the pasteButton
    /*pasteButton.addEventListener('click', async function (event) {
        if (isContextMenuVisible) {
            if (!popup.contains(event.target) && event.target !== popup) {
                hideContextMenu();
            }
            else {
                clipboard = await getClipboardContent();
                if (clipboard === "") return;
                addObject(clipboard);
                console.log(additionalObjects);
                hideContextMenu();
            }
        }
    });*/
    
    // C/Esc Key For Context Menu Visibility
    /*document.addEventListener('keydown', function (event) {
        if (event.key === 'c') showContextMenu(mouseX, mouseY);
        if (event.key === 'Escape') hideContextMenu();
    });*/
});

let isContextMenuVisible = false;

let canvasData = [];
let canvasProps = {};
let isDrawing = false; // New flag for drawing

function setup() {
    createCanvas(windowWidth, windowHeight);
    angleMode(DEGREES);
    loadCanvasData();
}

function drawDashedLine(x1, y1, x2, y2, dashLength) {
    const distance = dist(x1, y1, x2, y2);
    const numDashes = floor(distance / dashLength);
    for (let i = 0; i < numDashes; i++) {
        const xStart = lerp(x1, x2, i / numDashes);
        const yStart = lerp(y1, y2, i / numDashes);
        const xEnd = lerp(x1, x2, (i + 0.5) / numDashes);
        const yEnd = lerp(y1, y2, (i + 0.5) / numDashes);
        line(xStart, yStart, xEnd, yEnd);
    }
}

async function loadCanvasData() {
    try {
        const response = await fetch("/api/canvas_data");
        if (response.ok) {
            canvasData = await response.json();
            for (const item of canvasData) {
                if (item.Type === "CanvasProps") {
                    canvasProps = item;
                    break;
                }
            }
            drawItems();
        } else {
            console.error("Failed to fetch canvas data");
        }
    } catch (error) {
        console.error(error);
    }
}

let strokes = [];
let currentStroke = [];
let imageCache = {}; // to cache loaded images
function drawItems() {
    background(canvasProps.backgroundColor || '#fff'); // Canvas background

    for (const item of canvasData) {
        if (item.Type === "Rectangle") {
            const x = item.x * width;
            const y = item.y * height;
            const cx = x + item.width / 2;
            const cy = y + item.height / 2;
            stroke(item.strokeColor || "#000");
            strokeWeight(4);
            noFill();
            push();
            translate(cx, cy);
            rotate(item.rotation);

            // Draw dashed lines for the rectangle
            drawDashedLine(-item.width / 2, -item.height / 2, item.width / 2, -item.height / 2, 20);
            drawDashedLine(item.width / 2, -item.height / 2, item.width / 2, item.height / 2, 20);
            drawDashedLine(item.width / 2, item.height / 2, -item.width / 2, item.height / 2, 20);
            drawDashedLine(-item.width / 2, item.height / 2, -item.width / 2, -item.height / 2, 20);

            pop();
        } else if (item.Type === "Image") {
            const x = item.x * width;
            const y = item.y * height;
            let img = imageCache[item.ImageSRC];
            if (img) {
                image(img, x, y, item.width, item.height);
            }
            else {
                loadImage(item.ImageSRC, (loadedImg) => {
                    imageCache[item.ImageSRC] = loadedImg; // store the loaded image in the cache
                    image(loadedImg, x, y, item.width, item.height); // draw the loaded image
                });
            }
        } else if (item.Type === "Text") {
            const x = item.x * width;
            const y = item.y * height;
            noStroke();
            fill(item.textColor || "#000"); // Use given text color or default to black
            textSize(item.size);
            
            if(item.boundWidth && item.boundWidth) {
                const boundW = item.boundWidth * width; // Assuming boundWidth is relative to canvas width
                const words = item.value.split(' ');
                let line = '';
                let yOffset = 0;
                for(const word of words) {
                    const testLine = line + word + ' ';
                    const testWidth = textWidth(testLine);
                    if(testWidth > boundW && line.length > 0) {
                        text(line, x, y + yOffset);
                        line = word + ' ';
                        yOffset += textSize(); // or another value depending on desired line spacing
                    } else {
                        line = testLine;
                    }
                }
                text(line, x, y + yOffset); // Draw the last line
            } else {
                text(item.value, x, y);
            }
        } else if (item.Type === "CanvasProps") {
            // Draw the signing rectangle
            const signX = item.signX * width;
            const signY = item.signY * height;
            const cx = signX + item.signWidth / 2;
            const cy = signY + item.signHeight / 2;
            stroke(item.signColor || '#000');
            strokeWeight(item.signThickness || 4);
            fill(item.signBG || 'rgba(255, 255, 255, 0.5)');
            push();
            translate(cx, cy);
            rotate(item.signRotation || 0);
            rect(-item.signWidth / 2, -item.signHeight / 2, item.signWidth, item.signHeight);
            pop();

            
            stroke(0);
            strokeWeight(4);
            noFill();
            for (const stroke of strokes) {
                beginShape();
                for (const point of stroke) {
                    vertex(point.x, point.y);
                }
                endShape();
            }
        
            // Also draw the currentStroke
            if(isDrawing && currentStroke.length > 0) {
                beginShape();
                for(const point of currentStroke) {
                    vertex(point.x, point.y);
                }
                endShape();
            }
        }
    }

    for (const item of additionalObjects) {
        if (item.Type === "Image") {
            const x = item.x * width;
            const y = item.y * height;

            let img = imageCache[item.ImageSRC];
            if (img) {
                let originalWidth = img.width;
                let originalHeight = img.height;
                
                let newWidth = item.width; // the new width you want to set
                let aspectRatio = originalWidth / originalHeight; 
                let newHeight = newWidth / aspectRatio;
                // rotate the image
                push();
                translate(x, y);
                rotate(item.rotation);
                image(img, 0, 0, newWidth, newHeight);
                pop();
            } else {
                // If not, load the image, store it in the cache, and then draw it
                loadImage('/fetch_image?url=' + item.ImageSRC, (loadedImg) => {
                    let originalWidth = loadedImg.width;
                    let originalHeight = loadedImg.height;
                    
                    let newWidth = item.width; // the new width you want to set
                    let aspectRatio = originalWidth / originalHeight; 
                    let newHeight = newWidth / aspectRatio; // calculate the new height based on the aspect ratio
                    
                    imageCache[item.ImageSRC] = loadedImg; // store the loaded image in the cache
                    for (var i = 0; i < additionalObjects.length; i++) {
                        if (additionalObjects[i].ImageSRC === item.ImageSRC) {
                            additionalObjects[i].width = newWidth;
                            additionalObjects[i].height = newHeight;
                        }
                    }
                    
                });
            }
        } else if (item.Type === "Text") {
            const x = item.x * width;
            const y = item.y * height;
            noStroke();
            fill(item.textColor || "#000");
            textSize(item.size * item.scale);
            
            push();
            translate(x, y);
            rotate(item.rotation);
            text(item.value, 0, 0);
            pop();
        }
    }
}

function draw() {
    cursorX = mouseX;
    cursorY = mouseY;

    drawItems();
}


function isWithinSigningBounds() {
    const signX = canvasProps.signX * width;
    const signY = canvasProps.signY * height;
    const signWidth = canvasProps.signWidth;
    const signHeight = canvasProps.signHeight;

    return mouseX >= signX && mouseX <= (signX + signWidth) &&
        mouseY >= signY && mouseY <= (signY + signHeight);
}

function mousePressed() {
    if (isWithinSigningBounds) {
        isDrawing = true;
        currentStroke = [{ x: mouseX, y: mouseY }];
    }
}

function mouseDragged() {
    if (isDrawing && isWithinSigningBounds()) {
        currentStroke.push({ x: mouseX, y: mouseY });
    }
}

function mouseReleased() {
    if (isDrawing && currentStroke.length > 0) {
        strokes.push(currentStroke);
        currentStroke = [];
    }
    isDrawing = false;
}

let additionalObjects = [];

function addObject(str) {
    if (str.startsWith("IMGURL_")) {
        const imgURL = str.replace("IMGURL_", "");
        const imageObject = {
            Type: "Image",
            ImageSRC: imgURL,
            x: mouseX / width,
            y: mouseY / height,
            width: 200,
            rotation: 38,
            scale: 1
        };
        additionalObjects.push(imageObject);
    } else {
        const textObject = {
            Type: "Text",
            value: str,
            x: mouseX / width,
            y: mouseY / height,
            size: 16,
            textColor: "#000",
            boundWidth: 'max',
            rotation: 34,
            scale: 2
        };
        additionalObjects.push(textObject);
    }
}


function mouseMoved() {
    const objectUnderCursor = getObjectUnderCursor();
    if (objectUnderCursor) {
      console.log('Object Under Cursor:', objectUnderCursor);
    } else {
        console.log("No object under cursor");
    }
  }

  function degreesToRadians(degrees) {
      return degrees * (Math.PI / 180);
  }

  function pointInRotatedRectangle(point, rectPos, rectSize, rectAngleDegrees) {
    let rectAngleRadians = degreesToRadians(rectAngleDegrees);

    // Convert point coordinates to rectangle's local coordinate space
    let translatedPoint = createVector(point.x - rectPos.x, point.y - rectPos.y);
    let rotatedPoint = translatedPoint.copy().rotate(-rectAngleRadians);

    // Check if the rotated point is within the bounds of the rotated rectangle
    if (
        rotatedPoint.x > -rectSize.x / 2 &&
        rotatedPoint.x < rectSize.x / 2 &&
        rotatedPoint.y > -rectSize.y / 2 &&
        rotatedPoint.y < rectSize.y / 2
    ) {
        return true;
    } else {
        return false;
    }
}
  
  function getObjectUnderCursor() {
    for (const object of additionalObjects) {
        console.log("Hello");
      let { x, y, rotation, scale, id, width: objectWidth, height: objectHeight } = object;
      
      x = x * width;
      y = y * height;
      objectWidth = objectWidth * scale;
      objectHeight = objectHeight * scale;   
      point = { x : mouseX, y : mouseY };
        rectPos = { x, y };
        rectSize = { x: objectWidth, y: objectHeight };
        rectAngleDegrees = rotation;
        if (pointInRotatedRectangle(point, rectPos, rectSize, rectAngleDegrees)) {
            return object;
        }
    }
    return false;
}