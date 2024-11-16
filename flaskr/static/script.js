var events = {
  '#search-pixabay:click': SearchPixabay,
  '#pixabay-results:scroll': ScrollPixabayResults
}

for (var key in events) {
  var split_key = key.split(':')
  document.querySelectorAll(split_key[0]).forEach(element => {
    console.log(element, split_key[0], split_key[1])
    element.addEventListener(split_key[1], events[key])
  })
}

function Pixabay(term, page) {
  return new Promise(resolve => {
    term = term.trim().split(' ').join('+')
    var xhr = new XMLHttpRequest()
    xhr.open('GET', `https://pixabay.com/api/?key=25483695-93658ed46b8876fc2d6419379&q=${term}&per_page=25&image_type=vector&page=${page}`)
    xhr.addEventListener('load', function() {
      resolve(JSON.parse(this.response))
    })
    xhr.send()
  })
}

function SearchPixabay(event) {
  console.log(event)
  var elem = document.getElementById('pixabay-search')
  var term = elem.value
  var page = elem.dataset.page
  Pixabay(term, page).then(json => {
    var results = document.getElementById('pixabay-results')
    for (var i = 0; i < json.hits.length; i++) {
      var div = document.createElement('div')
      div.classList.add('pixabay-result')
      div.style.background = `url(${json.hits[i].largeImageURL})`
      MakeDraggable(div)
      results.appendChild(div)
    }
    elem.dataset.page = +page + 1
  })
}

function ScrollPixabayResults(event) {
  const scrollableElement = event.target;
  const isAtBottom = 
    scrollableElement.scrollHeight - scrollableElement.scrollTop <= scrollableElement.clientHeight;

  if (isAtBottom) {
    SearchPixabay();
  }
}

function SaveImage(event) {
  return new Promise(resolve => {
    var url = event.srcElement.style.background.replace('url(', '').replace(')', '')
    var xhr = new XMLHttpRequest()
    xhr.open('POST', '/api/save-image')
    xhr.addEventListener('load', function() {
      var res = JSON.parse(this.response)
      resolve(res)
    })
    xhr.send(url)
  })
}

function MakeDraggable(element) {
  $(element).draggable({
    start: function(event, ui) {
      
    },
    stop: function(event, ui) {
      
    },
    revert: "invalid"
  })
}

function MakeDroppable(element) {
   $(element).droppable({
    drop: function(event, ui) {

    },
    over: function(event, ui) {
      $(event.target).addClass("over")
    },
    out: function(event, ui) {
      $(event.target).removeClass("out")
    }
  })
}

$('.block').each(function(_, element) {
  MakeDroppable(element)
})

$('#tabs').tabs()
