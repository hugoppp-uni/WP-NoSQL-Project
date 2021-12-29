<template>
  <div class="hello">
    <h1>Twitter Hashtag Lookup App</h1>
    <h3>Hashtag Search</h3>
    <div>
      <input type="text" placeholder="Hashtag or interest" v-model="hashtagInput" />
      <button @click="searchHashtag">Search</button>
    </div>
    <div v-if=userInputReceived>
      Temp Log:
      <br>
      {{hashtagInput}}
      <br>
    {{apiCallFoo}}
      <br>
    </div>
  </div>
</template>

<script>
import axios from 'axios'

export default {
  name: 'StartPage',
  props: {
    msg: String
  },
  computed: {
    userInputReceived: function (){ //Tmp function
      return (this.hashtagInput.length > 1)
    }
  },
  data() {
    return {
      hashtagInput: "",
      apiCallFoo: ""
    };
  },
  methods: {
    searchHashtag() {
      axios
          .get('https://jsonplaceholder.typicode.com/todos/1')
          .then(res => {
            this.apiCallFoo = res.data;
          })

    },
  }
}
</script>

<!-- Add "scoped" attribute to limit CSS to this component only -->
<style scoped>
h3 {
  margin: 40px 0 0;
}
ul {
  list-style-type: none;
  padding: 0;
}
li {
  display: inline-block;
  margin: 0 10px;
}
a {
  color: #42b983;
}
</style>
